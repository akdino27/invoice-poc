namespace invoice_v1.src.Application.DTOs
{
    public class InvalidInvoiceDto
    {
        public Guid Id { get; set; }
        public string? FileId { get; set; }
        public string? FileName { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
