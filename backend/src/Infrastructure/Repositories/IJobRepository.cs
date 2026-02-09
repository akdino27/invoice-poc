using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IJobRepository
    {
        Task<JobQueue> CreateJobAsync(JobQueue job);
        Task<JobQueue?> GetByIdAsync(Guid id);
        Task<(List<JobQueue> Jobs, int Total)> GetJobsAsync(
            JobStatus? status,
            int skip,
            int take,
            string? vendorEmail = null);

        Task<List<JobQueue>> GetPendingJobsAsync(int batchSize);
        Task UpdateJobAsync(JobQueue job);
        Task<int> GetJobCountAsync(JobStatus? status = null);
    }
}
