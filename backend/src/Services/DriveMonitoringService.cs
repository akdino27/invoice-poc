using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace invoice_v1.src.Services
{
    // Background service that monitors Google Drive folder for file changes.

    public class DriveMonitoringService : BackgroundService
    {
        private readonly ILogger<DriveMonitoringService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        private readonly ConcurrentDictionary<string, DateTime> _lastSeenFiles = new();

        private readonly HashSet<string> _allowedMimeTypes = new()
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/jpeg",
            "image/png",
            "text/csv",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        public DriveMonitoringService(
            ILogger<DriveMonitoringService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Drive Monitoring Service is starting");

            try
            {
                await HydrateDictionaryFromDatabase(stoppingToken);

                using var timer = new PeriodicTimer(_interval);

                await DoWork(stoppingToken);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await DoWork(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Drive monitoring operation was cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during monitoring tick");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Drive Monitoring Service is stopping gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Drive Monitoring Service encountered a fatal error");
                throw;
            }

            _logger.LogInformation("Drive Monitoring Service has stopped");
        }

        private async Task HydrateDictionaryFromDatabase(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var lastSeenFiles = await dbContext.FileChangeLogs
                    .Where(log => log.FileId != null &&
                                  log.GoogleDriveModifiedTime != null &&
                                  (log.ChangeType == "Upload" || log.ChangeType == "Modified"))
                    .GroupBy(log => log.FileId)
                    .Select(group => new
                    {
                        FileId = group.Key,
                        ModifiedTime = group
                            .OrderByDescending(log => log.GoogleDriveModifiedTime)
                            .Select(log => log.GoogleDriveModifiedTime!.Value)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var file in lastSeenFiles.Where(f => f.FileId != null))
                {
                    _lastSeenFiles.TryAdd(file.FileId!, file.ModifiedTime);
                }

                _logger.LogInformation(
                    "Hydrated {Count} files from database into tracking dictionary",
                    _lastSeenFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hydrating dictionary from database");
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Drive Monitoring check at: {Time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var driveService = scope.ServiceProvider.GetRequiredService<IGoogleDriveService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var folderId = config["GoogleDrive:SharedFolderId"];

                if (string.IsNullOrEmpty(folderId))
                {
                    _logger.LogWarning("Shared folder ID not configured");
                    return;
                }

                var files = await driveService.ListFilesInFolderAsync(folderId, cancellationToken);

                if (files.Count == 0)
                {
                    _logger.LogInformation("No files found in folder");
                    await DetectDeletedFiles(new Dictionary<string, DateTime>(), dbContext, cancellationToken);
                    return;
                }

                var allCurrentFiles = files
                    .Where(f => f.ModifiedTime.HasValue)
                    .ToDictionary(f => f.Id, f => f.ModifiedTime!.Value);

                var logsToAdd = new List<FileChangeLog>();

                foreach (var file in files)
                {
                    bool isAllowed = !string.IsNullOrEmpty(file.MimeType) &&
                                    _allowedMimeTypes.Contains(file.MimeType);

                    if (!isAllowed)
                    {
                        _logger.LogWarning(
                            "File {FileName} with MIME type {MimeType} is not allowed and will be skipped",
                            file.Name,
                            file.MimeType);

                        _lastSeenFiles.TryRemove(file.Id, out _);
                        continue;
                    }

                    var fileModifiedTime = file.ModifiedTime ?? DateTime.MinValue;

                    if (!_lastSeenFiles.ContainsKey(file.Id))
                    {
                        logsToAdd.Add(new FileChangeLog
                        {
                            FileName = file.Name,
                            FileId = file.Id,
                            ChangeType = "Upload",
                            DetectedAt = DateTime.UtcNow,
                            MimeType = file.MimeType,
                            FileSize = file.Size,
                            ModifiedBy = file.Owners?.FirstOrDefault()?.DisplayName ?? "Unknown",
                            GoogleDriveModifiedTime = fileModifiedTime
                        });

                        _logger.LogInformation("New file detected: {FileName}", file.Name);
                    }
                    else if (_lastSeenFiles.TryGetValue(file.Id, out var lastSeenTime) &&
                             lastSeenTime < fileModifiedTime)
                    {
                        logsToAdd.Add(new FileChangeLog
                        {
                            FileName = file.Name,
                            FileId = file.Id,
                            ChangeType = "Modified",
                            DetectedAt = DateTime.UtcNow,
                            MimeType = file.MimeType,
                            FileSize = file.Size,
                            ModifiedBy = file.Owners?.FirstOrDefault()?.DisplayName ?? "Unknown",
                            GoogleDriveModifiedTime = fileModifiedTime
                        });

                        _logger.LogInformation("File modified: {FileName}", file.Name);
                    }
                }

                if (logsToAdd.Any())
                {
                    dbContext.FileChangeLogs.AddRange(logsToAdd);
                }

                await DetectDeletedFiles(allCurrentFiles, dbContext, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);

                var allowedCurrentFiles = files
                    .Where(f => f.MimeType != null &&
                               _allowedMimeTypes.Contains(f.MimeType) &&
                               f.ModifiedTime.HasValue)
                    .ToDictionary(f => f.Id, f => f.ModifiedTime!.Value);

                foreach (var kvp in allowedCurrentFiles)
                {
                    _lastSeenFiles.AddOrUpdate(kvp.Key, kvp.Value, (key, oldValue) => kvp.Value);
                }

                var deletedFileIds = _lastSeenFiles.Keys
                    .Except(allCurrentFiles.Keys)
                    .ToList();

                foreach (var fileId in deletedFileIds)
                {
                    _lastSeenFiles.TryRemove(fileId, out _);
                }

                _logger.LogInformation(
                    "Monitoring check completed. Tracked {Count} files, {Deleted} deleted",
                    _lastSeenFiles.Count,
                    deletedFileIds.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring check");
                throw;
            }
        }

        private async Task DetectDeletedFiles(
            Dictionary<string, DateTime> currentFiles,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var deletedFileIds = _lastSeenFiles.Keys
                .Where(oldFileId => !currentFiles.ContainsKey(oldFileId))
                .ToList();

            foreach (var oldFileId in deletedFileIds)
            {
                var changeLog = new FileChangeLog
                {
                    FileId = oldFileId,
                    ChangeType = "Deleted",
                    DetectedAt = DateTime.UtcNow
                };

                dbContext.FileChangeLogs.Add(changeLog);
                _logger.LogInformation("File deleted: {FileId}", oldFileId);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Drive Monitoring Service is stopping...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Drive Monitoring Service stopped gracefully");
        }
    }
}
