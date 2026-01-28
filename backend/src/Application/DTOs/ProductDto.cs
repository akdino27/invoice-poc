namespace invoice_v1.src.Application.DTOs
{
    // Basic Product DTO for CRUD operations and product catalog display
    public class ProductDto
    {
        public Guid Id { get; set; }

        // Business product identifier
        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? PrimaryCategory { get; set; }

        public string? SecondaryCategory { get; set; }

        public decimal? DefaultUnitRate { get; set; }

        // Aggregated statistics (for catalog display)
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime? LastSoldDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Paginated response for product list
    public class ProductListResponse
    {
        public List<ProductDto> Products { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
