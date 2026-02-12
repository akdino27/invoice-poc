using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using System.Text.Json;


namespace invoice_v1.src.Application.Interfaces
{
    // Service interface for job management operations.
    public interface IJobService
    {
        Task<JobDto> CreateJobFromLogAsync(FileChangeLog log);
        Task<JobDto?> GetJobByIdAsync(Guid jobId);
        Task<(List<JobDto> Jobs, int Total)> GetJobsAsync(JobStatus? status, int page, int pageSize);
        Task MarkProcessingAsync(Guid jobId, string workerId);
        Task MarkCompletedAsync(Guid jobId, object result);
        Task MarkInvalidAsync(Guid jobId, JsonDocument reason);
        Task MarkFailedAsync(Guid jobId, JsonDocument error);

    Task RequeueJobAsync(Guid jobId);
    }
}
