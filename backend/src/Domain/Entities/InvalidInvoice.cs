using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    public class InvalidInvoice
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(100)]
        public string? FileId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
