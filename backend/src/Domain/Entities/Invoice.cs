using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace invoice_v1.src.Domain.Entities
{
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; }

        public DateTime? InvoiceDate { get; set; }

        [MaxLength(100)]
        public string? OrderId { get; set; }

        // Vendor/Seller information
        [MaxLength(200)]
        public string? VendorName { get; set; }

        // Bill To (Customer)
        [MaxLength(200)]
        public string? BillToName { get; set; }

        // Ship To Address
        [MaxLength(200)]
        public string? ShipToCity { get; set; }

        [MaxLength(100)]
        public string? ShipToState { get; set; }

        [MaxLength(100)]
        public string? ShipToCountry { get; set; }

        [MaxLength(50)]
        public string? ShipMode { get; set; }

        // Financial Details
        public decimal? Subtotal { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? ShippingCost { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? BalanceDue { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        // Notes and Terms (simple text)
        public string? Notes { get; set; }
        public string? Terms { get; set; }

        // Drive file reference
        [Required]
        [MaxLength(100)]
        public string DriveFileId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Raw extracted / OCR / ML data for the invoice.
        /// Stored as jsonb in PostgreSQL.
        /// </summary>
        public JsonDocument? ExtractedDataJson { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoiceLine> LineItems { get; set; } = new List<InvoiceLine>();
    }
}
