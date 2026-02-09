using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    public class JobQueue
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string JobType { get; set; } = "EXTRACT_INVOICE";

        [Required]
        [Column(TypeName = "jsonb")]
        public string PayloadJson { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public int RetryCount { get; set; } = 0;

        [MaxLength(200)]
        public string? LockedBy { get; set; }

        public DateTime? LockedAt { get; set; }

        public DateTime? NextRetryAt { get; set; }

        public string? ErrorMessage { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
