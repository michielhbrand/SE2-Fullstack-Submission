namespace ManagementApi.Models;

public class Organization
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? TaxNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public bool Active { get; set; } = true;
    
    // Foreign key to Address
    public int? AddressId { get; set; }
    public Address? Address { get; set; }
    
    // Store bank account IDs as comma-separated values
    public List<int> BankAccountIds { get; set; } = new();
    
    // Navigation property for organization members
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
