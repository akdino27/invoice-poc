namespace invoice_v1.src.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? PrimaryCategory { get; set; }
        public string? SecondaryCategory { get; set; }
        public decimal? DefaultUnitRate { get; set; }
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime? LastSoldDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ProductListResponse
    {
        public List<ProductDto> Products { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
