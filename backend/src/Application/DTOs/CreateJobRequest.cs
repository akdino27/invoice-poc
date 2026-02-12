using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Application.DTOs
{
    // Request DTO for manual job creation (admin endpoint).
    public class CreateJobRequest
    {
        [Required]
        [MaxLength(100)]
        public string FileId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(200)]
        public string? MimeType { get; set; }
    }
}
