namespace invoice_v1.src.Application.DTOs
{
    /// <summary>
    /// Data transfer object for vendor information.
    /// </summary>
    public class VendorDto
    {
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastActivityAt { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// Paginated response for vendor listings (admin only).
    /// </summary>
    public class VendorListResponse
    {
        public List<VendorDto> Vendors { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }
}
