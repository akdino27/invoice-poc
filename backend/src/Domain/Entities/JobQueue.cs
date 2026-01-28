using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    /// <summary>
    /// Represents a job in the processing queue.
    /// Jobs are created by the backend from FileChangeLogs and claimed by workers.
    /// </summary>
    public class JobQueue
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string JobType { get; set; } = nameof(Enums.JobType.INVOICE_EXTRACTION);

        /// <summary>
        /// JSON payload containing fileId, originalName, mimeType, etc.
        /// Schema defined in contracts/job_payload_schema.json
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string PayloadJson { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = nameof(Enums.JobStatus.PENDING);

        public int RetryCount { get; set; } = 0;

        [MaxLength(200)]
        public string? LockedBy { get; set; }

        public DateTime? LockedAt { get; set; }

        public DateTime? NextRetryAt { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ErrorMessage { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
