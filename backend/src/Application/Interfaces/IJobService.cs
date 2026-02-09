using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IJobService
    {
        Task<JobDto> CreateJobFromLogAsync(FileChangeLog log);
        Task<JobDto?> GetJobByIdAsync(Guid jobId, string? userEmail = null, bool isAdmin = false);
        Task<(List<JobDto> Jobs, int Total)> GetJobsAsync(
            JobStatus? status,
            int page,
            int pageSize,
            string? userEmail = null,
            bool isAdmin = false);

        Task MarkProcessingAsync(Guid jobId, string workerId);
        Task MarkCompletedAsync(Guid jobId, object result);
        Task MarkInvalidAsync(Guid jobId, string reason);
        Task MarkFailedAsync(Guid jobId, string errorMessage);
        Task RequeueJobAsync(Guid jobId);
    }
}
