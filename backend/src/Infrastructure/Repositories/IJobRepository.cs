using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IJobRepository
    {
        Task<JobQueue> CreateAsync(JobQueue job);
        Task<JobQueue?> GetByIdAsync(Guid id);
        Task<List<JobQueue>> GetAllAsync(JobStatus? status, int skip, int take);
        Task<int> GetCountAsync(JobStatus? status);
        Task UpdateJobAsync(JobQueue job); 
        Task<int> GetPendingCountAsync();
    }
}
