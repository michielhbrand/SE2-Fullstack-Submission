namespace ManagementApi.Models;

/// <summary>
/// Represents a user in the system. User data is synchronized with Keycloak.
/// The UserId is the Keycloak user ID (UUID).
/// </summary>
public class User
{
    // This is the Keycloak user ID (UUID string), used as primary key
    public required string Id { get; set; }
    
    public required string Email { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public bool Active { get; set; } = true;
    
    // Navigation property for organization memberships
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}
