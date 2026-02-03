using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace invoice_v1.src.Domain.Entities
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? PrimaryCategory { get; set; }

        [MaxLength(100)]
        public string? SecondaryCategory { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DefaultUnitRate { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalQuantitySold { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; } = 0;
        public int InvoiceCount { get; set; } = 0;

        public DateTime? LastSoldDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    }
}
