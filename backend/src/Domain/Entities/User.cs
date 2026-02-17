using invoice_v1.src.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty; 
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }

        public int FailedLoginCount { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public Guid? ApprovedByAdminId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }

        public bool IsSoftDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
