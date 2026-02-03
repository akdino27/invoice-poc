namespace invoice_v1.src.Application.DTOs
{
    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? OrderId { get; set; }
        public string? VendorName { get; set; }

        // Bill To
        public string? BillToName { get; set; }

        // Ship To
        public ShipToDto? ShipTo { get; set; }

        public string? ShipMode { get; set; }

        // Financial
        public decimal? Subtotal { get; set; }
        public DiscountDto? Discount { get; set; }
        public decimal? ShippingCost { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? BalanceDue { get; set; }
        public string? Currency { get; set; }

        // Additional
        public string? Notes { get; set; }
        public string? Terms { get; set; }

        // File reference
        public string DriveFileId { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }

        // Extracted data
        public object? ExtractedData { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InvoiceLineDto>? LineItems { get; set; }
    }
    // Ship To address information
    public class ShipToDto
    {
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }

    // Discount information
    public class DiscountDto
    {
        public decimal? Percentage { get; set; }
        public decimal? Amount { get; set; }
    }

    // Data transfer object for invoice line items
    public class InvoiceLineDto
    {
        public Guid Id { get; set; }

        // Product reference
        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? Category { get; set; }

        // Line details
        public decimal? Quantity { get; set; }
        public decimal? UnitRate { get; set; }
        public decimal? Amount { get; set; }
    }
}
