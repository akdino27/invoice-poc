using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Application.DTOs
{
    public class CallbackRequest
    {
        [Required(ErrorMessage = "JobId is required")]
        public Guid JobId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("COMPLETED|INVALID|FAILED", ErrorMessage = "Status must be COMPLETED, INVALID, or FAILED")]
        public string Status { get; set; } = string.Empty;

        public object? Result { get; set; }

        [MaxLength(2000)]
        public string? Reason { get; set; }

        [MaxLength(200)]
        public string? WorkerId { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
