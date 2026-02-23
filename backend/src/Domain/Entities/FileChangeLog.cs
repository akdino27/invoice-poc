namespace invoice_v1.src.Domain.Entities
{
    public class FileChangeLog
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? FileId { get; set; }
        public string? ChangeType { get; set; }
        public DateTime DetectedAt { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? GoogleDriveModifiedTime { get; set; }
        public bool Processed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }
        public Guid? UploadedByVendorId { get; set; }

        // Security pipeline fields
        public string SecurityStatus { get; set; } = "Pending";
        public string? SecurityFailReason { get; set; }
        public DateTime? SecurityCheckedAt { get; set; }
    }
}
