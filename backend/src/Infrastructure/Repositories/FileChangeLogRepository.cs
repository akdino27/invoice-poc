using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class FileChangeLogRepository : IFileChangeLogRepository
    {
        private readonly ApplicationDbContext _context;

        public FileChangeLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FileChangeLog?> GetByIdAsync(int id)
        {
            return await _context.FileChangeLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<FileChangeLog?> GetLatestByFileIdAsync(string fileId)
        {
            return await _context.FileChangeLogs
                .AsNoTracking()
                .Where(f => f.FileId == fileId)
                .OrderByDescending(f => f.DetectedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<FileChangeLog>> GetLogsAsync(
            Guid? vendorId,
            string? changeType,
            int skip,
            int take)
        {
            var query = _context.FileChangeLogs.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(l => l.UploadedByVendorId == vendorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(changeType))
            {
                query = query.Where(l => l.ChangeType == changeType);
            }

            return await query
                .OrderByDescending(l => l.DetectedAt)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetLogCountAsync(Guid? vendorId, string? changeType)
        {
            var query = _context.FileChangeLogs.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(l => l.UploadedByVendorId == vendorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(changeType))
            {
                query = query.Where(l => l.ChangeType == changeType);
            }

            return await query.CountAsync();
        }

        public async Task<List<(string ChangeType, int Count, int Processed, int Pending)>> GetLogStatsAsync(Guid? vendorId)
        {
            var query = _context.FileChangeLogs.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(l => l.UploadedByVendorId == vendorId.Value);
            }

            return await query
                .GroupBy(l => l.ChangeType)
                .Select(g => new ValueTuple<string, int, int, int>(
                    g.Key ?? string.Empty,
                    g.Count(),
                    g.Count(l => l.Processed),
                    g.Count(l => !l.Processed)))
                .ToListAsync();
        }

        public async Task<List<FileChangeLog>> GetUnprocessedLogsAsync(int limit)
        {
            return await _context.FileChangeLogs
                .Where(log => !log.Processed &&
                             log.FileId != null &&
                             (log.ChangeType == "Upload" || log.ChangeType == "Modified"))
                .OrderBy(log => log.DetectedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<FileChangeLog> CreateAsync(FileChangeLog log)
        {
            _context.FileChangeLogs.Add(log);
            return log;
        }

        public Task UpdateAsync(FileChangeLog log)
        {
            _context.FileChangeLogs.Update(log);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
