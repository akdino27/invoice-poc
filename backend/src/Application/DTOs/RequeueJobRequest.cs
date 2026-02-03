using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Application.DTOs
{
    // Request DTO for requeuing a failed job.
    public class RequeueJobRequest
    {
        [Required]
        public Guid JobId { get; set; }
    }
}
