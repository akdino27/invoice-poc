using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IJobQueueRepository
    {
        Task<JobQueue?> GetByIdAsync(Guid id);
        Task<List<JobQueue>> GetJobsAsync(JobStatus? status, Guid? vendorId, int skip, int take);
        Task<int> GetJobCountAsync(JobStatus? status, Guid? vendorId);
        Task<bool> CanVendorAccessJobAsync(Guid jobId, Guid vendorId);
        Task<JobQueue> CreateAsync(JobQueue job);
        Task UpdateAsync(JobQueue job);
        Task<int> SaveChangesAsync();
    }

    public class JobQueueRepository : IJobQueueRepository
    {
        private readonly ApplicationDbContext _context;

        public JobQueueRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JobQueue?> GetByIdAsync(Guid id)
        {
            return await _context.JobQueues
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<List<JobQueue>> GetJobsAsync(JobStatus? status, Guid? vendorId, int skip, int take)
        {
            var query = _context.JobQueues.AsQueryable();

            if (status.HasValue)
            {
                var statusString = status.Value.ToString().ToUpper();
                query = query.Where(j => j.Status == statusString);
            }

            if (vendorId.HasValue)
            {
                var vendorIdString = vendorId.Value.ToString();
                query = query.Where(j =>
                    j.PayloadJson != null &&
                    j.PayloadJson.RootElement.GetProperty("vendorId").GetString() == vendorIdString);
            }

            return await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetJobCountAsync(JobStatus? status, Guid? vendorId)
        {
            var query = _context.JobQueues.AsQueryable();

            if (status.HasValue)
            {
                var statusString = status.Value.ToString().ToUpper();
                query = query.Where(j => j.Status == statusString);
            }

            if (vendorId.HasValue)
            {
                var vendorIdString = vendorId.Value.ToString();
                query = query.Where(j =>
                    j.PayloadJson != null &&
                    j.PayloadJson.RootElement.GetProperty("vendorId").GetString() == vendorIdString);
            }

            return await query.CountAsync();
        }

        public async Task<bool> CanVendorAccessJobAsync(Guid jobId, Guid vendorId)
        {
            var job = await _context.JobQueues
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job?.PayloadJson == null)
                return false;

            var vendorIdString = vendorId.ToString();
            return job.PayloadJson.RootElement.TryGetProperty("vendorId", out var vId) &&
                   vId.GetString() == vendorIdString;
        }

        public async Task<JobQueue> CreateAsync(JobQueue job)
        {
            _context.JobQueues.Add(job);
            return job;
        }

        public Task UpdateAsync(JobQueue job)
        {
            _context.JobQueues.Update(job);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
