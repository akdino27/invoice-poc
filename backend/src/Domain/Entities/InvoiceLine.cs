using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    public class InvoiceLine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;  

        [Required]
        public Guid ProductGuid { get; set; }

        [ForeignKey(nameof(ProductGuid))]
        public Product Product { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
