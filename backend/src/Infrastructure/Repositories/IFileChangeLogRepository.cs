using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IFileChangeLogRepository
    {
        Task<FileChangeLog> CreateAsync(FileChangeLog log);
        Task CreateRangeAsync(List<FileChangeLog> logs); 
        Task<FileChangeLog?> GetByIdAsync(Guid id);
        Task<FileChangeLog?> GetByFileIdAsync(string fileId);
        Task<(List<FileChangeLog> Logs, int Total)> GetLogsAsync(
            int skip,
            int take,
            string? vendorEmail = null);

        Task<List<FileChangeLog>> GetUnprocessedAsync(int batchSize);
        Task MarkAsProcessedAsync(Guid id);
        Task<int> GetUnprocessedCountAsync();
    }
}
