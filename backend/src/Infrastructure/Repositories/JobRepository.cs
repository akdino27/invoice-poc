using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    /// <summary>
    /// Job repository with RBAC support.
    /// UPDATED: Added vendorEmail filtering to GetJobsAsync.
    /// </summary>
    public class JobRepository : IJobRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(ApplicationDbContext context, ILogger<JobRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<JobQueue> CreateJobAsync(JobQueue job)
        {
            _context.JobQueues.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<JobQueue?> GetByIdAsync(Guid id)
        {
            return await _context.JobQueues.FindAsync(id);
        }

        /// <summary>
        /// UPDATED: Added vendorEmail parameter for RBAC filtering.
        /// </summary>
        public async Task<(List<JobQueue> Jobs, int Total)> GetJobsAsync(
            JobStatus? status,
            int skip,
            int take,
            string? vendorEmail = null)
        {
            var query = _context.JobQueues.AsQueryable();

            // Filter by status if provided
            if (status.HasValue)
            {
                var statusString = status.Value.ToString();
                query = query.Where(j => j.Status == statusString);
            }

            // RBAC filtering: Filter by vendor email from job payload
            if (!string.IsNullOrWhiteSpace(vendorEmail))
            {
                // Filter jobs where PayloadJson contains the vendor's email
                // This assumes the payload has a "modifiedBy" field with the vendor email
                query = query.Where(j => j.PayloadJson.Contains(vendorEmail));
            }

            var total = await query.CountAsync();

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (jobs, total);
        }

        public async Task<List<JobQueue>> GetPendingJobsAsync(int batchSize)
        {
            var now = DateTime.UtcNow;

            return await _context.JobQueues
                .Where(j => j.Status == "PENDING" ||
                           (j.Status == "FAILED" &&
                            j.NextRetryAt.HasValue &&
                            j.NextRetryAt.Value <= now))
                .Where(j => j.LockedBy == null || j.LockedAt == null)
                .OrderBy(j => j.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task UpdateJobAsync(JobQueue job)
        {
            job.UpdatedAt = DateTime.UtcNow;
            _context.JobQueues.Update(job);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetJobCountAsync(JobStatus? status = null)
        {
            var query = _context.JobQueues.AsQueryable();

            if (status.HasValue)
            {
                var statusString = status.Value.ToString();
                query = query.Where(j => j.Status == statusString);
            }

            return await query.CountAsync();
        }
    }
}
