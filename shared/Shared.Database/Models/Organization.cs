using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class Organization
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public required string Name { get; set; }
    
    public string? TaxNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public bool Active { get; set; } = true;
    
    public int? AddressId { get; set; }
    
    [ForeignKey(nameof(AddressId))]
    public Address? Address { get; set; }
    
    public List<int> BankAccountIds { get; set; } = new();
    
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
