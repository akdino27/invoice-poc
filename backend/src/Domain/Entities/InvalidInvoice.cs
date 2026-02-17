using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace invoice_v1.src.Domain.Entities
{
    public class InvalidInvoice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid JobId { get; set; } // Link to source Job

        [MaxLength(200)]
        public string FileId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? FileName { get; set; }

        public Guid? VendorId { get; set; } // For filtering

        [Column(TypeName = "jsonb")]
        public JsonDocument? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
