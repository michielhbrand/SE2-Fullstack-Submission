namespace ManagementApi.Models;

/// <summary>
/// Represents the relationship between a Keycloak user and an organization.
/// UserId references a user in Keycloak (not a database foreign key).
/// </summary>
public class OrganizationMember
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    // This is a Keycloak user ID (UUID string), not a database foreign key
    public required string UserId { get; set; }
    
    public required string Role { get; set; } // e.g., "owner", "admin", "member"
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
