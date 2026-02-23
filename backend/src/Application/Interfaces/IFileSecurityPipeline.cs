using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IFileSecurityPipeline
    {
        Task<SecurityPipelineResult> RunAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid vendorId);
    }

    public class SecurityPipelineResult
    {
        public bool IsHealthy { get; set; }
        public string? FailReason { get; set; }
        public SecurityFailReason? FailCode { get; set; }
    }
}
