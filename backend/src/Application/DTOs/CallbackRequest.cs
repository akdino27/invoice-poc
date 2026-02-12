using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Application.DTOs
{
    // Request DTO for worker callback endpoint.
    // Schema defined in contracts/callback_schema.json
    public class CallbackRequest
    {
        [Required(ErrorMessage = "JobId is required")]
        public Guid JobId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(COMPLETED|INVALID|FAILED)$", ErrorMessage = "Status must be COMPLETED, INVALID, or FAILED")]
        public string Status { get; set; } = string.Empty;
        // Extracted invoice data (for COMPLETED status).
        public object? Result { get; set; }

        // Reason for failure/invalidity (for INVALID/FAILED status).
        [MaxLength(2000)]
        public string? Reason { get; set; }

        [MaxLength(200)]
        public string? WorkerId { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
