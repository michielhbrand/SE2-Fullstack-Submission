namespace InvoiceTrackerApi.Models;

/// <summary>
/// Minimal user table storing only business/state fields.
/// Identity data (firstname, lastname, email, roles) is owned by Keycloak.
/// The Id is the Keycloak user ID (UUID).
/// </summary>
public class User
{
    /// <summary>
    /// Keycloak user ID (UUID string), used as primary key
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Business state: whether the user is active in the application
    /// </summary>
    public bool Active { get; set; } = true;
    
    /// <summary>
    /// Timestamp when the user was created in the app database
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when the user was last updated in the app database
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property for organization memberships
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
}
