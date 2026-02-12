using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(ApplicationDbContext context, ILogger<JobRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<JobQueue?> GetByIdAsync(Guid id)
        {
            return await _context.JobQueues
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<List<JobQueue>> GetJobsAsync(JobStatus? status, int skip, int take)
        {
            var query = _context.JobQueues.AsNoTracking().AsQueryable();

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

        public async Task<int> GetJobCountAsync(JobStatus? status)
        {
            var query = _context.JobQueues.AsQueryable();

            if (status.HasValue)
            {
                var statusString = status.Value.ToString();
                query = query.Where(j => j.Status == statusString);
            }

            return await query.CountAsync();
        }

        public async Task<JobQueue> CreateJobAsync(JobQueue job)
        {
            _context.JobQueues.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created job {JobId} of type {JobType}", job.Id, job.JobType);

            return job;
        }

        public async Task UpdateJobAsync(JobQueue job)
        {
            job.UpdatedAt = DateTime.UtcNow;
            _context.JobQueues.Update(job);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Updated job {JobId} to status {Status}", job.Id, job.Status);
        }

        public async Task<List<FileChangeLog>> GetUnprocessedFileChangeLogsAsync(int limit)
        {
            return await _context.FileChangeLogs
                .Where(log => !log.Processed &&
                             (log.ChangeType == "Upload" || log.ChangeType == "Modified") &&
                             log.FileId != null)
                .OrderBy(log => log.DetectedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task MarkFileChangeLogAsProcessedAsync(int logId)
        {
            var log = await _context.FileChangeLogs.FindAsync(logId);
            if (log != null)
            {
                log.Processed = true;
                log.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogDebug("Marked FileChangeLog {LogId} as processed", logId);
            }
        }
    }
}
