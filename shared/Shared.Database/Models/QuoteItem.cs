using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class QuoteItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuoteId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerUnit { get; set; }

    [NotMapped]
    public decimal TotalPrice => Quantity * PricePerUnit;
}
