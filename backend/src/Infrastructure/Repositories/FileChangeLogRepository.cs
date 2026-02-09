using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class FileChangeLogRepository : IFileChangeLogRepository
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<FileChangeLogRepository> logger;

        public FileChangeLogRepository(
            ApplicationDbContext context,
            ILogger<FileChangeLogRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<FileChangeLog> CreateAsync(FileChangeLog log)
        {
            try
            {
                context.FileChangeLogs.Add(log);
                await context.SaveChangesAsync();
                logger.LogDebug("Created FileChangeLog {Id} for file {FileId}", log.Id, log.FileId);
                return log;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "FAILED to save FileChangeLog for file {FileId}, ChangeType: {ChangeType}, FileName: {FileName}",
                    log.FileId, log.ChangeType, log.FileName);
                throw;
            }
        }

        public async Task CreateRangeAsync(List<FileChangeLog> logs)
        {
            try
            {
                context.FileChangeLogs.AddRange(logs);
                var saved = await context.SaveChangesAsync();
                logger.LogInformation("Saved {Count} FileChangeLogs to database", saved);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FAILED to save {Count} FileChangeLogs", logs.Count);
                throw;
            }
        }


        public async Task<List<FileChangeLog>> GetUnprocessedAsync(int batchSize = 50)
        {
            return await context.FileChangeLogs
                .Where(log => log.ProcessedAt == null &&
                             (log.ChangeType == "Upload" || log.ChangeType == "Modified"))
                .OrderBy(log => log.DetectedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(Guid logId)
        {
            var log = await context.FileChangeLogs.FindAsync(logId);
            if (log != null)
            {
                log.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<FileChangeLog>> GetAllAsync(int skip, int take)
        {
            return await context.FileChangeLogs
                .OrderByDescending(l => l.DetectedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await context.FileChangeLogs.CountAsync();
        }
    }
}
