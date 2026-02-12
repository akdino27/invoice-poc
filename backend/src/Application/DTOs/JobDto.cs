namespace invoice_v1.src.Application.DTOs
{
    // Data transfer object for job information.
    public class JobDto
    {
        public Guid Id { get; set; }
        public string JobType { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Paginated response for job listings.
    public class JobListResponse
    {
        public List<JobDto> Jobs { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    }
}
