using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IJobRepository
    {
        Task<JobQueue?> GetByIdAsync(Guid id);
        Task<List<JobQueue>> GetJobsByFileIdAsync(string fileId); // FIX: Added
        Task<(List<JobQueue> jobs, int total)> GetJobsAsync(
            JobStatus? status,
            int page,
            int pageSize,
            Guid? vendorId);
        Task CreateAsync(JobQueue job);
        Task UpdateAsync(JobQueue job);
        Task SaveChangesAsync();
    }
}
