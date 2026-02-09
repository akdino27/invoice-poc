using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    // Represents a line item in an invoice.
    // Links Invoice to Product with quantity, price, and amount details.
    public class InvoiceLine
    {
        // Primary key: GUID.
        [Key]
        public Guid Id { get; set; }
        // Foreign key to Invoice.
        
        [Required]
        public Guid InvoiceId { get; set; }
        // Navigation property to parent Invoice.
        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;
        // Foreign key to Product (GUID reference).
        // This is the internal Product.Id, not the business ProductId.
        [Required]
        public Guid ProductGuid { get; set; }
        // Navigation property to Product entity.
        [ForeignKey(nameof(ProductGuid))]
        public Product Product { get; set; } = null!;

        
        // Business product identifier (denormalized for query performance).
        // Same as Product.ProductId.
        // Example: FUR-TA-3775
        
        [Required]
        [MaxLength(100)]
        public string ProductId { get; set; } = string.Empty;

        
        // Product name (denormalized for historical accuracy).
        // Even if Product.ProductName changes later, this preserves what was on the invoice.
        
        [Required]
        [MaxLength(500)]
        public string ProductName { get; set; } = string.Empty;

        
        // Product category (denormalized for historical accuracy).
        
        [MaxLength(200)]
        public string? Category { get; set; }

        
        // Quantity ordered.
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Quantity { get; set; }

        
        // Unit price at time of invoice.
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitRate { get; set; }

        
        // Line total (Quantity * UnitRate).
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        
        // Record creation timestamp.
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
