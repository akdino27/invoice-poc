namespace invoice_v1.src.Application.DTOs
{
    public class UploadResult
    {
        public bool Success { get; set; }
        public string? FileId { get; set; }
        public string? Message { get; set; }
        public string? SecurityReason { get; set; }
    }
}
