using System.Text.Json;

namespace invoice_v1.src.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? OrderId { get; set; }
        public string? VendorName { get; set; }
        public string? BillToName { get; set; }
        public string? ShipToCity { get; set; }
        public string? ShipToState { get; set; }
        public string? ShipToCountry { get; set; }
        public string? ShipMode { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? ShippingCost { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? BalanceDue { get; set; }
        public string? Currency { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public string DriveFileId { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public JsonDocument? ExtractedDataJson { get; set; }
        public Guid? UploadedByVendorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InvoiceLine> LineItems { get; set; } = new();
        public User? UploadedByVendor { get; set; }
    }
}
