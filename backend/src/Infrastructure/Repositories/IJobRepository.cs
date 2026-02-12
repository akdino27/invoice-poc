using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IJobRepository
    {
        Task<JobQueue?> GetByIdAsync(Guid id);
        Task<List<JobQueue>> GetJobsAsync(JobStatus? status, int skip, int take);
        Task<int> GetJobCountAsync(JobStatus? status);
        Task<JobQueue> CreateJobAsync(JobQueue job);
        Task UpdateJobAsync(JobQueue job);
        Task<List<FileChangeLog>> GetUnprocessedFileChangeLogsAsync(int limit);
        Task MarkFileChangeLogAsProcessedAsync(int logId);
    }
}
