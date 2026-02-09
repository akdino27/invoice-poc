using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    
    // Represents a product discovered from invoices.
    // Products are VENDOR-SPECIFIC: Same ProductId from different vendors = different Product records.
    // Unique constraint: (VendorEmail, ProductId).
    
    public class Product
    {
        // Primary key: GUID for internal database operations.
        [Key]
        public Guid Id { get; set; }
        
        // Foreign key to Vendor table.
        // Email address of the vendor who uploaded invoices containing this product.
        // RBAC: Used to filter products per vendor.
        [Required]
        [MaxLength(200)]
        public string VendorEmail { get; set; } = string.Empty;
        // Navigation property to Vendor entity.
        [ForeignKey(nameof(VendorEmail))]
        public Vendor Vendor { get; set; } = null!;
        // Business product identifier (e.g., FUR-TA-3775).
        // This is the product code from the invoice.
        // Unique constraint: (VendorEmail, ProductId).
        // Different vendors can have same ProductId - they are treated as separate products.
        [Required]
        [MaxLength(100)]
        public string ProductId { get; set; } = string.Empty;
        // Product name/description.
        [Required]
        [MaxLength(500)]
        public string ProductName { get; set; } = string.Empty;    
        // Full category string (e.g., "Furniture, Chairs").
        [MaxLength(200)]
        public string? Category { get; set; }
        // Primary category (first level, e.g., "Furniture").
        [MaxLength(100)]
        public string? PrimaryCategory { get; set; }
        // Secondary category (second level, e.g., "Chairs").
        [MaxLength(100)]
        public string? SecondaryCategory { get; set; }
        // Most recent unit rate for this product.
        // Updated whenever product appears in a new invoice.
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DefaultUnitRate { get; set; }
        // Cumulative quantity sold across all invoices for this vendor.
        // Aggregated statistic.
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalQuantitySold { get; set; } = 0;
        // Cumulative revenue from this product for this vendor.
        // Aggregated statistic.

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; } = 0;
        // Number of invoices containing this product for this vendor.
        // Aggregated statistic. 
        public int InvoiceCount { get; set; } = 0;
        // Most recent invoice date where this product appeared.
        public DateTime? LastSoldDate { get; set; }
        // Timestamp when product was first discovered.
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Last update timestamp (when aggregates were updated).
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation property: All invoice line items referencing this product.
        public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    }
}
