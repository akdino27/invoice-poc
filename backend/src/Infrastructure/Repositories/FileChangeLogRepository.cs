using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class FileChangeLogRepository : IFileChangeLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileChangeLogRepository> _logger;

        public FileChangeLogRepository(
            ApplicationDbContext context,
            ILogger<FileChangeLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FileChangeLog> CreateAsync(FileChangeLog log)
        {
            _context.FileChangeLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task CreateRangeAsync(List<FileChangeLog> logs)
        {
            _context.FileChangeLogs.AddRange(logs);
            await _context.SaveChangesAsync();
        }

        public async Task<FileChangeLog?> GetByIdAsync(Guid id)
        {
            return await _context.FileChangeLogs.FindAsync(id);
        }

        public async Task<FileChangeLog?> GetByFileIdAsync(string fileId)
        {
            return await _context.FileChangeLogs
                .Where(l => l.FileId == fileId)
                .OrderByDescending(l => l.DetectedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<FileChangeLog> Logs, int Total)> GetLogsAsync(
            int skip,
            int take,
            string? vendorEmail = null)
        {
            var query = _context.FileChangeLogs.AsQueryable();

            // RBAC filtering: Filter by ModifiedBy for non-admins
            if (!string.IsNullOrWhiteSpace(vendorEmail))
            {
                query = query.Where(l => l.ModifiedBy == vendorEmail);
            }

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.DetectedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (logs, total);
        }

        public async Task<List<FileChangeLog>> GetUnprocessedAsync(int batchSize)
        {
            return await _context.FileChangeLogs
                .Where(l => l.ProcessedAt == null)
                .OrderBy(l => l.DetectedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(Guid id)
        {
            var log = await _context.FileChangeLogs.FindAsync(id);
            if (log != null)
            {
                log.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnprocessedCountAsync()
        {
            return await _context.FileChangeLogs
                .Where(l => l.ProcessedAt == null)
                .CountAsync();
        }
    }
}
