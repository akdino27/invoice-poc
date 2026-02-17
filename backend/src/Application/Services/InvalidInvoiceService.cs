using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;
using invoice_v1.src.Domain.Enums; // Assuming JobStatus is here

namespace invoice_v1.src.Application.Services
{
    public class InvalidInvoiceService : IInvalidInvoiceService
    {
        private readonly IJobRepository _jobRepository;

        public InvalidInvoiceService(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<object> GetInvalidInvoicesAsync(int page, int pageSize, Guid? vendorId)
        {
            // We assume "Invalid" invoice means a Job with Status = FAILED (or INVALID if enum exists)
            // Querying for Failed jobs
            var (jobs, totalCount) = await _jobRepository.GetJobsAsync(
                JobStatus.FAILED,
                page,
                pageSize,
                vendorId
            );

            // Map to a DTO suitable for the frontend view
            var dtos = jobs.Select(j => new
            {
                Id = j.Id,
                FileName = GetFileNameFromPayload(j.PayloadJson),
                VendorId = GetVendorIdFromPayload(j.PayloadJson),
                ErrorMessage = j.ErrorMessage?.RootElement.ToString(),
                CreatedAt = j.CreatedAt,
                FailedAt = j.UpdatedAt,
                RetryCount = j.RetryCount,
                Status = j.Status
            });

            return new
            {
                Data = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private string GetFileNameFromPayload(System.Text.Json.JsonDocument? payload)
        {
            if (payload == null) return "Unknown File";
            if (payload.RootElement.TryGetProperty("originalName", out var name))
            {
                return name.GetString() ?? "Unknown File";
            }
            return "Unknown File";
        }

        private string GetVendorIdFromPayload(System.Text.Json.JsonDocument? payload)
        {
            if (payload == null) return "";
            if (payload.RootElement.TryGetProperty("vendorId", out var id))
            {
                return id.GetString() ?? "";
            }
            return "";
        }
    }
}
