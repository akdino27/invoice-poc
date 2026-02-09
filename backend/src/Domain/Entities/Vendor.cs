using System.ComponentModel.DataAnnotations;

namespace invoice_v1.src.Domain.Entities
{
    // Primary key is the email address from Google Drive file owner.
    public class Vendor
    {
        // This is the primary key for vendor identification.
        [Key]
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;
        // Display name extracted from email or Google Drive displayName.
        [MaxLength(200)]
        public string? DisplayName { get; set; }
        // Timestamp when vendor first uploaded an invoice.
        [Required]
        public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
        // Timestamp of vendor's last activity (invoice upload).
        [Required]
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        // Navigation property: All invoices uploaded by this vendor.
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        // Navigation property: All products discovered from this vendor's invoices.
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
