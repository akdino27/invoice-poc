namespace invoice_v1.src.Application.DTOs
{
    public class CategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
