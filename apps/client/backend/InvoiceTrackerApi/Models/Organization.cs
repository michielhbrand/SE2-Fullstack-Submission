namespace InvoiceTrackerApi.Models;

public class Organization
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public List<int> BankAccountIds { get; set; } = new();
    
    /// <summary>
    /// Navigation property for organization members (many-to-many with Keycloak users)
    /// </summary>
    public List<OrganizationMember> Members { get; set; } = new();
}
