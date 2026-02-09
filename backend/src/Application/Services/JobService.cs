using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Repositories;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    // Implements job management business logic.
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<JobService> _logger;
        private const int MaxRetries = 3;

        public JobService(IJobRepository jobRepository, ILogger<JobService> logger)
        {
            _jobRepository = jobRepository;
            _logger = logger;
        }

        public async Task<JobDto> CreateJobFromLogAsync(FileChangeLog log)
        {
            if (string.IsNullOrWhiteSpace(log.FileId))
            {
                throw new ArgumentException("FileId cannot be empty", nameof(log));
            }

            // Create structured payload conforming to job_payload_schema.json
            var payload = new
            {
                fileId = log.FileId,
                originalName = log.FileName,
                mimeType = log.MimeType,
                fileSize = log.FileSize,
                uploader = log.ModifiedBy,
                schemaVersion = "1.0",
                idempotencyKey = $"{log.FileId}_{log.DetectedAt:yyyyMMddHHmmss}",
                detectedAt = log.DetectedAt
            };

            var job = new JobQueue
            {
                Id = Guid.NewGuid(),
                JobType = nameof(JobType.INVOICE_EXTRACTION),
                PayloadJson = JsonDocument.Parse(JsonSerializer.Serialize(payload)),
                Status = nameof(JobStatus.PENDING),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _jobRepository.CreateJobAsync(job);
            await _jobRepository.MarkFileChangeLogAsProcessedAsync(log.Id);

            _logger.LogInformation(
                "Created job {JobId} for file {FileId} ({FileName})",
                created.Id,
                log.FileId,
                log.FileName);

            return MapToDto(created);
        }

        public async Task<JobDto?> GetJobByIdAsync(Guid jobId)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            return job != null ? MapToDto(job) : null;
        }

        public async Task<(List<JobDto> Jobs, int Total)> GetJobsAsync(
            JobStatus? status,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;
            var jobs = await _jobRepository.GetJobsAsync(status, skip, pageSize);
            var total = await _jobRepository.GetJobCountAsync(status);

            return (jobs.Select(MapToDto).ToList(), total);
        }

        public async Task MarkProcessingAsync(Guid jobId, string workerId)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            if (job.Status != nameof(JobStatus.PENDING))
            {
                throw new InvalidOperationException(
                    $"Job {jobId} cannot be marked as PROCESSING. Current status: {job.Status}");
            }

            job.Status = nameof(JobStatus.PROCESSING);
            job.LockedBy = workerId;
            job.LockedAt = DateTime.UtcNow;

            await _jobRepository.UpdateJobAsync(job);

            _logger.LogInformation(
                "Job {JobId} marked as PROCESSING by worker {WorkerId}",
                jobId,
                workerId);
        }

        public async Task MarkCompletedAsync(Guid jobId, object result)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            job.Status = nameof(JobStatus.COMPLETED);
            job.ErrorMessage = null;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobRepository.UpdateJobAsync(job);

            _logger.LogInformation("Job {JobId} marked as COMPLETED", jobId);
        }

        public async Task MarkInvalidAsync(Guid jobId, JsonDocument reason)
        {
            var job = await _jobRepository.GetByIdAsync(jobId)
                ?? throw new InvalidOperationException($"Job {jobId} not found");

            job.Status = nameof(JobStatus.INVALID);
            job.ErrorMessage = reason;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobRepository.UpdateJobAsync(job);
        }


        public async Task MarkFailedAsync(Guid jobId, JsonDocument error)
        {
            var job = await _jobRepository.GetByIdAsync(jobId)
                ?? throw new InvalidOperationException($"Job {jobId} not found");

            job.ErrorMessage = error;
            job.RetryCount++;

            // Exponential backoff for retries
            if (job.RetryCount < MaxRetries)
            {
                //Change status to PENDING (was FAILED)
                job.Status = nameof(JobStatus.PENDING);

                // Release lock so worker can claim it
                job.LockedBy = null;
                job.LockedAt = null;

                var backoffMinutes = Math.Pow(2, job.RetryCount);
                job.NextRetryAt = DateTime.UtcNow.AddMinutes(backoffMinutes);

                _logger.LogWarning(
                    "Job {JobId} scheduled for retry {RetryCount}/{MaxRetries}. Next retry at {NextRetryAt}",
                    jobId,
                    job.RetryCount,
                    MaxRetries,
                    job.NextRetryAt);
            }
            else
            {
                //Only set to FAILED permanently after max retries
                job.Status = nameof(JobStatus.FAILED);
                job.LockedBy = null;
                job.LockedAt = null;

                _logger.LogError(
                    "Job {JobId} marked as FAILED permanently after {RetryCount} retries",
                    jobId,
                    job.RetryCount);
            }

            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateJobAsync(job);
        }


        public async Task RequeueJobAsync(Guid jobId)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            job.Status = nameof(JobStatus.PENDING);
            job.LockedBy = null;
            job.LockedAt = null;
            job.NextRetryAt = null;
            job.ErrorMessage = null;
            job.RetryCount = 0;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobRepository.UpdateJobAsync(job);

            _logger.LogInformation("Job {JobId} requeued by admin", jobId);
        }

        private static JobDto MapToDto(JobQueue job)
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
                payload = job.PayloadJson;
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
