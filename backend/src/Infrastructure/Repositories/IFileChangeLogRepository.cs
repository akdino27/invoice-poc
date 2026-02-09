using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IFileChangeLogRepository
    {
        Task<FileChangeLog> CreateAsync(FileChangeLog log);
        Task CreateRangeAsync(List<FileChangeLog> logs);
        Task<List<FileChangeLog>> GetUnprocessedAsync(int batchSize = 50);
        Task MarkAsProcessedAsync(Guid logId); 
        Task<List<FileChangeLog>> GetAllAsync(int skip, int take);
        Task<int> GetCountAsync();
    }
}
