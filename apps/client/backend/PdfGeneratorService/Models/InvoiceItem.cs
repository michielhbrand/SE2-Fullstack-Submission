using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdfGeneratorService.Models;

public class InvoiceItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Amount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerUnit { get; set; }

    [NotMapped]
    public decimal TotalPrice => Amount * PricePerUnit;

    // Navigation property
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }
}
