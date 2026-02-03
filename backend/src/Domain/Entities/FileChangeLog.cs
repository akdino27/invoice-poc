using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Domain.Entities
{
    public class FileChangeLog
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(100)]
        public string? FileId { get; set; }

        [MaxLength(50)]
        public string? ChangeType { get; set; }

        [Required]
        public DateTime DetectedAt { get; set; }

        [MaxLength(200)]
        public string? MimeType { get; set; }

        public long? FileSize { get; set; }

        [MaxLength(200)]
        public string? ModifiedBy { get; set; }

        public DateTime? GoogleDriveModifiedTime { get; set; }

        // Indicates if this log has been processed into a JobQueue entry.
        public bool Processed { get; set; } = false;

        public DateTime? ProcessedAt { get; set; }
    }
}
