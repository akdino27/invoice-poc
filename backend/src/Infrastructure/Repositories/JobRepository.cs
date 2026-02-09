using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly ApplicationDbContext context;

        public JobRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<JobQueue> CreateAsync(JobQueue job)
        {
            context.JobQueues.Add(job);
            await context.SaveChangesAsync();
            return job;
        }

        public async Task<JobQueue?> GetByIdAsync(Guid id)
        {
            return await context.JobQueues.FindAsync(id);
        }

        public async Task<List<JobQueue>> GetAllAsync(JobStatus? status, int skip, int take)
        {
            var query = context.JobQueues.AsQueryable();

            if (status.HasValue)
            {
                var statusString = status.Value.ToString();
                query = query.Where(j => j.Status == statusString);
            }

            return await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync(JobStatus? status)
        {
            var query = context.JobQueues.AsQueryable();

            if (status.HasValue)
            {
                var statusString = status.Value.ToString();
                query = query.Where(j => j.Status == statusString);
            }

            return await query.CountAsync();
        }

        public async Task UpdateJobAsync(JobQueue job)
        {
            context.Entry(job).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await context.JobQueues
                .Where(j => j.Status == "PENDING")
                .CountAsync();
        }
    }
}
