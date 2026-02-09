using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceTrackerApi.Models;

public class Organization
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }
    
    [Required]
    public int AddressId { get; set; }
    
    [ForeignKey(nameof(AddressId))]
    public Address? Address { get; set; }
    
    public List<int> BankAccountIds { get; set; } = new();
    
    public List<OrganizationMember> Members { get; set; } = new();
}
