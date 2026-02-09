using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.Models;

public class BankAccount
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string BankName { get; set; }
    
    [Required]
    [MaxLength(20)]
    public required string BranchCode { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string AccountNumber { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string AccountType { get; set; }
    
    [Required]
    public bool Active { get; set; } = true;
    
    [Required]
    public int OrganizationId { get; set; }
}
