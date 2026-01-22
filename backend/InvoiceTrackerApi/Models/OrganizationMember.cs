namespace InvoiceTrackerApi.Models;

/// <summary>
/// Join table for many-to-many relationship between Keycloak users and Organizations.
/// Stores only the Keycloak user ID (sub claim) without creating a local users table.
/// </summary>
public class OrganizationMember
{
    /// <summary>
    /// Foreign key to Organization
    /// </summary>
    public int OrganizationId { get; set; }
    
    /// <summary>
    /// Keycloak user identifier (sub claim from JWT).
    /// This is NOT a foreign key - it references a user in Keycloak's database.
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// Role of the user within the organization (e.g., "orgAdmin", "orgUser")
    /// </summary>
    public required string Role { get; set; }
    
    /// <summary>
    /// Navigation property to Organization
    /// </summary>
    public Organization? Organization { get; set; }
}
