namespace invoice_v1.src.Application.DTOs
{
    public class FileChangeLogDto
    {
        public Guid Id { get; set; }  
        public string? FileName { get; set; }
        public string? FileId { get; set; }
        public string? ChangeType { get; set; }
        public DateTime DetectedAt { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? GoogleDriveModifiedTime { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
