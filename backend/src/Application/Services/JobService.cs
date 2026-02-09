using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Repositories;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    /// <summary>
    /// Job management service with RBAC support.
    /// UPDATED: Added RBAC filtering for GetJobsAsync and GetJobByIdAsync.
    /// </summary>
    public class JobService : IJobService
    {
        private readonly IJobRepository jobRepository;
        private readonly ILogger<JobService> logger;

        public JobService(IJobRepository jobRepository, ILogger<JobService> logger)
        {
            this.jobRepository = jobRepository;
            this.logger = logger;
        }

        public async Task<JobDto> CreateJobFromLogAsync(FileChangeLog log)
        {
            var payload = new
            {
                fileId = log.FileId,
                originalName = log.FileName,
                mimeType = log.MimeType,
                modifiedBy = log.ModifiedBy
            };

            var job = new JobQueue
            {
                Id = Guid.NewGuid(),
                JobType = "EXTRACT_INVOICE",
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await jobRepository.CreateJobAsync(job);

            logger.LogInformation("Created job {JobId} for file {FileId}", job.Id, log.FileId);

            return MapToDto(job);
        }

        public async Task<JobDto?> GetJobByIdAsync(Guid jobId, string? userEmail = null, bool isAdmin = false)
        {
            var job = await jobRepository.GetByIdAsync(jobId);

            if (job == null)
            {
                return null;
            }

            // RBAC: Non-admins can only access their own jobs
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                var jobVendorEmail = ExtractVendorEmailFromJob(job);
                if (!string.IsNullOrWhiteSpace(jobVendorEmail) &&
                    !jobVendorEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("User {UserEmail} attempted to access job {JobId} belonging to {JobVendorEmail}",
                        userEmail, jobId, jobVendorEmail);
                    return null; // Return null (404) instead of throwing exception
                }
            }

            return MapToDto(job);
        }

        public async Task<(List<JobDto> Jobs, int Total)> GetJobsAsync(
            JobStatus? status,
            int page,
            int pageSize,
            string? userEmail = null,
            bool isAdmin = false)
        {
            // RBAC: Non-admins only see their own jobs
            var vendorEmailFilter = isAdmin ? null : userEmail;

            var skip = (page - 1) * pageSize;

            var (jobs, total) = await jobRepository.GetJobsAsync(
                status,
                skip,
                pageSize,
                vendorEmailFilter);

            var jobDtos = jobs.Select(MapToDto).ToList();

            return (jobDtos, total);
        }

        public async Task MarkProcessingAsync(Guid jobId, string workerId)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            job.Status = "PROCESSING";
            job.LockedBy = workerId;
            job.LockedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            await jobRepository.UpdateJobAsync(job);

            logger.LogInformation("Job {JobId} marked as PROCESSING by worker {WorkerId}", jobId, workerId);
        }

        public async Task MarkCompletedAsync(Guid jobId, object result)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            job.Status = "COMPLETED";
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            await jobRepository.UpdateJobAsync(job);

            logger.LogInformation("Job {JobId} marked as COMPLETED", jobId);
        }

        public async Task MarkInvalidAsync(Guid jobId, string reason)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            job.Status = "INVALID";
            job.ErrorMessage = reason;
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            await jobRepository.UpdateJobAsync(job);

            logger.LogWarning("Job {JobId} marked as INVALID: {Reason}", jobId, reason);
        }

        public async Task MarkFailedAsync(Guid jobId, string errorMessage)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            job.Status = "FAILED";
            job.RetryCount++;
            job.ErrorMessage = errorMessage;
            job.NextRetryAt = CalculateNextRetry(job.RetryCount);
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            await jobRepository.UpdateJobAsync(job);

            logger.LogWarning("Job {JobId} marked as FAILED (retry {RetryCount}): {ErrorMessage}",
                jobId, job.RetryCount, errorMessage);
        }

        public async Task RequeueJobAsync(Guid jobId)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            if (job.Status != "FAILED" && job.Status != "INVALID")
            {
                throw new InvalidOperationException($"Only FAILED or INVALID jobs can be requeued. Current status: {job.Status}");
            }

            job.Status = "PENDING";
            job.RetryCount = 0;
            job.ErrorMessage = null;
            job.NextRetryAt = null;
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            await jobRepository.UpdateJobAsync(job);

            logger.LogInformation("Job {JobId} requeued to PENDING status", jobId);
        }

        private JobDto MapToDto(JobQueue job)
        {
            object? payload = null;
            if (!string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                try
                {
                    payload = JsonSerializer.Deserialize<object>(job.PayloadJson);
                }
                catch
                {
                    payload = job.PayloadJson;
                }
            }

            return new JobDto
            {
                Id = job.Id,
                JobType = job.JobType,
                Payload = payload,
                Status = job.Status,
                RetryCount = job.RetryCount,
                LockedBy = job.LockedBy,
                LockedAt = job.LockedAt,
                NextRetryAt = job.NextRetryAt,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            };
        }

        private string? ExtractVendorEmailFromJob(JobQueue job)
        {
            if (string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                return null;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(job.PayloadJson);
                if (payload.TryGetProperty("modifiedBy", out var modifiedByElement))
                {
                    return modifiedByElement.GetString();
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        private DateTime? CalculateNextRetry(int retryCount)
        {
            // Exponential backoff: 2^retryCount minutes
            var delayMinutes = Math.Pow(2, retryCount);
            return DateTime.UtcNow.AddMinutes(delayMinutes);
        }
    }
}
