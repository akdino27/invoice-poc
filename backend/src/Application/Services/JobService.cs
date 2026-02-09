using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Repositories;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository jobRepository;
        private readonly ILogger<JobService> logger;
        private const int MaxRetries = 3;

        public JobService(
            IJobRepository jobRepository,
            ILogger<JobService> logger)
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
                fileSize = log.FileSize,
                modifiedBy = log.ModifiedBy,
                detectedAt = log.DetectedAt
            };

            var job = new JobQueue
            {
                Id = Guid.NewGuid(),
                JobType = nameof(JobType.INVOICE_EXTRACTION),
                PayloadJson = JsonDocument.Parse(JsonSerializer.Serialize(payload)),
                Status = nameof(JobStatus.PENDING),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                NextRetryAt = null
            };

            await jobRepository.CreateAsync(job);
            logger.LogInformation("Created job {JobId} for file {FileId}", job.Id, log.FileId);

            return MapToDto(job);
        }

        public async Task<JobDto?> GetJobByIdAsync(Guid jobId)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            return job != null ? MapToDto(job) : null;
        }

        public async Task<(List<JobDto> Jobs, int Total)> GetJobsAsync(
            JobStatus? status, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;
            var jobs = await jobRepository.GetAllAsync(status, skip, pageSize);
            var total = await jobRepository.GetCountAsync(status);

            var jobDtos = jobs.Select(MapToDto).ToList();
            return (jobDtos, total);
        }

        public async Task MarkProcessingAsync(Guid jobId, string workerId)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
                throw new InvalidOperationException($"Job {jobId} not found");

            job.Status = "PROCESSING";
            job.LockedBy = workerId;
            job.LockedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            await jobRepository.UpdateJobAsync(job);
            logger.LogInformation("Job {JobId} locked by worker {WorkerId}", jobId, workerId);
        }

        public async Task MarkCompletedAsync(Guid jobId, object result)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
                throw new InvalidOperationException($"Job {jobId} not found");

            job.Status = "COMPLETED";
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            await jobRepository.UpdateJobAsync(job);
            logger.LogInformation("Job {JobId} marked as COMPLETED", jobId);
        }

        public async Task MarkInvalidAsync(Guid jobId, JsonDocument reason)
        {
            var job = await _jobRepository.GetByIdAsync(jobId)
                ?? throw new InvalidOperationException($"Job {jobId} not found");

            job.Status = "INVALID";
            job.ErrorMessage = reason;
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            await _jobRepository.UpdateJobAsync(job);
        }


        public async Task MarkFailedAsync(Guid jobId, JsonDocument error)
        {
            var job = await _jobRepository.GetByIdAsync(jobId)
                ?? throw new InvalidOperationException($"Job {jobId} not found");

            job.ErrorMessage = error;
            job.RetryCount++;
            job.ErrorMessage = errorMessage;
            job.UpdatedAt = DateTime.UtcNow;
            job.LockedBy = null;
            job.LockedAt = null;

            if (job.RetryCount >= MaxRetries)
            {
                // Permanently failed after max retries
                job.Status = "FAILED";
                job.NextRetryAt = null;

                logger.LogError(
                    "Job {JobId} permanently FAILED after {RetryCount} attempts. Error: {Error}",
                    jobId, job.RetryCount, errorMessage);
            }
            else
            {
                // Calculate exponential backoff: 2^retryCount minutes
                var delayMinutes = Math.Pow(2, job.RetryCount);
                job.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                job.Status = "PENDING";

                logger.LogWarning(
                    "Job {JobId} marked as FAILED (attempt {RetryCount}/{MaxRetries}). " +
                    "Will retry at {NextRetryAt} (in {DelayMinutes} minutes). Error: {Error}",
                    jobId, job.RetryCount, MaxRetries, job.NextRetryAt, delayMinutes, errorMessage);
            }

            await jobRepository.UpdateJobAsync(job);
        }

        public async Task RequeueJobAsync(Guid jobId)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
                throw new InvalidOperationException($"Job {jobId} not found");

            if (job.Status != "FAILED" && job.Status != "INVALID")
                throw new InvalidOperationException($"Only FAILED or INVALID jobs can be requeued");

            job.Status = "PENDING";
            job.RetryCount = 0;
            job.ErrorMessage = null;
            job.LockedBy = null;
            job.LockedAt = null;
            job.NextRetryAt = null;
            job.UpdatedAt = DateTime.UtcNow;

            await jobRepository.UpdateJobAsync(job);
            logger.LogInformation("Job {JobId} requeued successfully", jobId);
        }

        private JobDto MapToDto(JobQueue job)
        {
            object? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<object>(
                    job.PayloadJson.RootElement.GetRawText()
                );

            }
            catch
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
                ErrorMessage = job.ErrorMessage != null
                ? job.ErrorMessage.RootElement.GetRawText()
                : null,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            };
        }
    }
}
