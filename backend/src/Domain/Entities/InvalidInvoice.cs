using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace invoice_v1.src.Domain.Entities
{
    public class InvalidInvoice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(100)]
        public string? FileId { get; set; }

        /// <summary>
        /// Structured reason why the invoice was invalid
        /// (validation errors, OCR issues, parsing failures, etc.)
        /// Stored as jsonb in PostgreSQL.
        /// </summary>
        [Required]
        public JsonDocument Reason { get; set; } = JsonDocument.Parse("{}");

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
