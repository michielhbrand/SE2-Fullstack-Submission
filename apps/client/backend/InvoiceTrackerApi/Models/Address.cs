using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.Models;

public class Address
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public required string FirstLine { get; set; }
    
    [MaxLength(255)]
    public string? SecondLine { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string City { get; set; }
    
    [Required]
    [MaxLength(20)]
    public required string Code { get; set; }
}
