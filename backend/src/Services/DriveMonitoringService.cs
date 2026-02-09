using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Services
{
    public class DriveMonitoringService : BackgroundService
    {
        private readonly ILogger<DriveMonitoringService> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan interval = TimeSpan.FromHours(1);

        private readonly ConcurrentDictionary<string, (string FileName, DateTime ModifiedTime)> lastSeenFiles = new();

        private readonly HashSet<string> allowedMimeTypes = new()
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
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Drive Monitoring Service is starting");

            try
            {
                await HydrateDictionaryFromDatabase(stoppingToken);

                using var timer = new PeriodicTimer(interval);

                await DoWork(stoppingToken);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await DoWork(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("Drive monitoring operation was cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during monitoring tick");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Drive Monitoring Service is stopping gracefully");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Drive Monitoring Service encountered a fatal error");
                throw;
            }

            logger.LogInformation("Drive Monitoring Service has stopped");
        }

        private async Task HydrateDictionaryFromDatabase(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var deletedFileIdsList = await dbContext.FileChangeLogs
                    .Where(log => log.ChangeType == "Deleted" && log.FileId != null)
                    .Select(log => log.FileId!)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var deletedFileIds = new HashSet<string>(deletedFileIdsList);

                logger.LogDebug("Found {Count} deleted file IDs to exclude from hydration", deletedFileIds.Count);

                var lastSeenFilesList = await dbContext.FileChangeLogs
                    .Where(log => log.FileId != null &&
                                  log.GoogleDriveModifiedTime != null &&
                                  log.FileName != null &&
                                  (log.ChangeType == "Upload" || log.ChangeType == "Modified") &&
                                  !deletedFileIds.Contains(log.FileId))
                    .GroupBy(log => log.FileId)
                    .Select(group => new
                    {
                        FileId = group.Key,
                        FileName = group
                            .OrderByDescending(log => log.GoogleDriveModifiedTime)
                            .Select(log => log.FileName)
                            .FirstOrDefault(),
                        ModifiedTime = group
                            .OrderByDescending(log => log.GoogleDriveModifiedTime)
                            .Select(log => log.GoogleDriveModifiedTime!.Value)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var file in lastSeenFilesList.Where(f => f.FileId != null && f.FileName != null))
                {
                    lastSeenFiles.TryAdd(file.FileId!, (file.FileName!, file.ModifiedTime));
                }

                logger.LogInformation(
                    "Hydrated {Count} active files into tracking dictionary (excluded {DeletedCount} deleted files)",
                    lastSeenFiles.Count, deletedFileIds.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error hydrating dictionary from database");
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            logger.LogInformation("Drive Monitoring check at {Time}", DateTimeOffset.Now);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var driveService = scope.ServiceProvider.GetRequiredService<IGoogleDriveService>();
                var fileChangeLogRepository = scope.ServiceProvider.GetRequiredService<IFileChangeLogRepository>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var folderId = config["GoogleDrive:SharedFolderId"];
                if (string.IsNullOrEmpty(folderId))
                {
                    logger.LogWarning("Shared folder ID not configured");
                    return;
                }

                var files = await driveService.ListFilesInFolderAsync(folderId, cancellationToken);

                if (files.Count == 0)
                {
                    logger.LogInformation("No files found in folder");
                    await DetectDeletedFiles(
                        new Dictionary<string, (string, DateTime)>(),
                        fileChangeLogRepository,
                        cancellationToken);
                    return;
                }

                logger.LogInformation("Listed {Count} files from folder {FolderId}", files.Count, folderId);

                var allCurrentFiles = files
                    .Where(f => f.ModifiedTime.HasValue && !string.IsNullOrEmpty(f.Name))
                    .ToDictionary(f => f.Id, f => (f.Name, f.ModifiedTime!.Value));

                var logsToAdd = new List<FileChangeLog>();

                foreach (var file in files)
                {
                    bool isAllowed = !string.IsNullOrEmpty(file.MimeType) &&
                                   allowedMimeTypes.Contains(file.MimeType);

                    if (!isAllowed)
                    {
                        logger.LogWarning(
                            "File {FileName} with MIME type {MimeType} is not allowed and will be skipped",
                            file.Name, file.MimeType);
                        lastSeenFiles.TryRemove(file.Id, out _);
                        continue;
                    }

                    var fileModifiedTime = file.ModifiedTime ?? DateTime.MinValue;

                    if (!lastSeenFiles.ContainsKey(file.Id))
                    {
                        logsToAdd.Add(new FileChangeLog
                        {
                            Id = Guid.NewGuid(),
                            FileName = file.Name,
                            FileId = file.Id,
                            ChangeType = "Upload",
                            DetectedAt = DateTime.UtcNow,
                            MimeType = file.MimeType,
                            FileSize = file.Size,
                            ModifiedBy = file.Owners?.FirstOrDefault()?.DisplayName ?? "Unknown",
                            GoogleDriveModifiedTime = fileModifiedTime
                        });
                        logger.LogInformation("New file detected: {FileName}", file.Name);
                    }
                    else if (lastSeenFiles.TryGetValue(file.Id, out var lastSeen) &&
                             lastSeen.ModifiedTime < fileModifiedTime)
                    {
                        logsToAdd.Add(new FileChangeLog
                        {
                            Id = Guid.NewGuid(),
                            FileName = file.Name,
                            FileId = file.Id,
                            ChangeType = "Modified",
                            DetectedAt = DateTime.UtcNow,
                            MimeType = file.MimeType,
                            FileSize = file.Size,
                            ModifiedBy = file.Owners?.FirstOrDefault()?.DisplayName ?? "Unknown",
                            GoogleDriveModifiedTime = fileModifiedTime
                        });
                        logger.LogInformation("File modified: {FileName}", file.Name);
                    }
                }

                if (logsToAdd.Any())
                {
                    await fileChangeLogRepository.CreateRangeAsync(logsToAdd);
                }

                await DetectDeletedFiles(allCurrentFiles, fileChangeLogRepository, cancellationToken);

                var allowedCurrentFiles = files
                    .Where(f => f.MimeType != null &&
                               allowedMimeTypes.Contains(f.MimeType) &&
                               f.ModifiedTime.HasValue &&
                               !string.IsNullOrEmpty(f.Name))
                    .ToDictionary(f => f.Id, f => (f.Name, f.ModifiedTime!.Value));

                foreach (var kvp in allowedCurrentFiles)
                {
                    lastSeenFiles.AddOrUpdate(kvp.Key, kvp.Value, (key, oldValue) => kvp.Value);
                }

                // Clean up tracking dictionary - remove files no longer in Drive
                var filesToRemove = lastSeenFiles.Keys.Except(allowedCurrentFiles.Keys).ToList();
                foreach (var fileId in filesToRemove)
                {
                    lastSeenFiles.TryRemove(fileId, out _);
                }

                logger.LogInformation(
                    "Monitoring check completed. Currently tracking {Count} active files",
                    lastSeenFiles.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during monitoring check");
                throw;
            }
        }

        private async Task DetectDeletedFiles(
            Dictionary<string, (string FileName, DateTime ModifiedTime)> currentFiles,
            IFileChangeLogRepository fileChangeLogRepository,
            CancellationToken cancellationToken)
        {
            var deletedFileIds = lastSeenFiles.Keys
                .Where(oldFileId => !currentFiles.ContainsKey(oldFileId))
                .ToList();

            if (!deletedFileIds.Any())
            {
                logger.LogDebug("No deleted files detected");
                return;
            }

            logger.LogInformation("Detected {Count} deleted files", deletedFileIds.Count);

            var deletionLogs = new List<FileChangeLog>();

            foreach (var deletedFileId in deletedFileIds)
            {
                var fileName = lastSeenFiles.TryGetValue(deletedFileId, out var fileInfo)
                    ? fileInfo.FileName
                    : "Unknown (deleted before tracking)";

                deletionLogs.Add(new FileChangeLog
                {
                    Id = Guid.NewGuid(),
                    FileId = deletedFileId,
                    FileName = fileName,
                    ChangeType = "Deleted",
                    DetectedAt = DateTime.UtcNow,
                    MimeType = null,
                    FileSize = null,
                    ModifiedBy = null,
                    GoogleDriveModifiedTime = null
                });

                logger.LogInformation("File deleted: {FileName} (FileId: {FileId})", fileName, deletedFileId);
            }

            try
            {
                await fileChangeLogRepository.CreateRangeAsync(deletionLogs);
                logger.LogInformation("Successfully saved {Count} deletion logs to database", deletionLogs.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FAILED to save {Count} deletion logs to database", deletionLogs.Count);
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Drive Monitoring Service is stopping...");
            await base.StopAsync(cancellationToken);
            logger.LogInformation("Drive Monitoring Service stopped gracefully");
        }
    }
}
