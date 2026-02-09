using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    
    // Tracks files that failed validation or processing.
    // Linked to vendor for RBAC (vendors can only see their own failed uploads).
    
    public class InvalidInvoice
    {
        
        // Primary key: GUID.
        
        [Key]
        public Guid Id { get; set; }

        
        // Foreign key to Vendor table (nullable for backward compatibility).
        // Email address of the vendor who uploaded this file.
        // Can be null if file was uploaded before vendor system was implemented.
        
        [MaxLength(200)]
        public string? VendorEmail { get; set; }

        
        // Navigation property to Vendor entity.
        
        [ForeignKey(nameof(VendorEmail))]
        public Vendor? Vendor { get; set; }

        
        // Google Drive file ID.
        
        [MaxLength(500)]
        public string? FileName { get; set; }

        
        // Original filename.
        
        [MaxLength(100)]
        public string? FileId { get; set; }

        
        // Reason for validation failure or processing error.
        // Examples: "InvoiceNumber is required", "Failed after 3 retry attempts"
        
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Reason { get; set; } = string.Empty;

        
        // Timestamp when marked as invalid.
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
