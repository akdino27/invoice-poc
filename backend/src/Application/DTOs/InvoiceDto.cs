namespace invoice_v1.src.Application.DTOs
{
    public class InvoiceDto
    {
        public Guid Id { get; set; }

        public string? VendorEmail { get; set; }

        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? OrderId { get; set; }
        public string? VendorName { get; set; }

        public string? BillToName { get; set; }

        public ShipToDto? ShipTo { get; set; }
        public string? ShipMode { get; set; }

        public decimal? Subtotal { get; set; }
        public DiscountDto? Discount { get; set; }
        public decimal? ShippingCost { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? BalanceDue { get; set; }
        public string? Currency { get; set; }

        public string? Notes { get; set; }
        public string? Terms { get; set; }

        public string DriveFileId { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }

        public object? ExtractedData { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InvoiceLineDto>? LineItems { get; set; }
    }

    public class ShipToDto
    {
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }

    public class DiscountDto
    {
        public decimal? Percentage { get; set; }
        public decimal? Amount { get; set; }
    }

    public class InvoiceLineDto
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitRate { get; set; }
        public decimal? Amount { get; set; }
    }
}
