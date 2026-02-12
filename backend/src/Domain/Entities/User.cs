using System.ComponentModel.DataAnnotations;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Domain.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        // -------------------------
        // Identity
        // -------------------------

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        public bool EmailVerified { get; set; } = false;

        // -------------------------
        // Authentication
        // -------------------------

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        public int FailedLoginCount { get; set; } = 0;

        public DateTime? LastLoginAt { get; set; }

        // -------------------------
        // Authorization
        // -------------------------

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public UserStatus Status { get; set; }

        // -------------------------
        // Approval / Audit
        // -------------------------

        public Guid? ApprovedByAdminId { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // -------------------------
        // Lifecycle
        // -------------------------

        public bool IsSoftDeleted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
