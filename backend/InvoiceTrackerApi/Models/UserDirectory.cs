namespace InvoiceTrackerApi.Models;

/// <summary>
/// UserDirectory read model - shared across all backends.
/// This is a denormalized table combining Keycloak identity data with app DB business/state data.
/// DO NOT write to this table directly - it is populated via sync from ManagementBackend.
/// </summary>
public class UserDirectory
{
    /// <summary>
    /// Keycloak user ID (UUID string), used as primary key
    /// </summary>
    public required string Id { get; set; }
    
    // ===== Identity data from Keycloak (read-only) =====
    
    /// <summary>
    /// User's email address from Keycloak
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// User's first name from Keycloak
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// User's last name from Keycloak
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Comma-separated list of roles from Keycloak
    /// </summary>
    public string? Roles { get; set; }
    
    /// <summary>
    /// Whether the user is enabled in Keycloak
    /// </summary>
    public bool KeycloakEnabled { get; set; } = true;
    
    // ===== Business/state data from app database =====
    
    /// <summary>
    /// Business state: whether the user is active in the application
    /// </summary>
    public bool Active { get; set; } = true;
    
    /// <summary>
    /// Timestamp when the user was created in the app database
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Timestamp when the user was last updated in the app database
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // ===== Sync metadata =====
    
    /// <summary>
    /// Timestamp when this directory entry was last synced from Keycloak
    /// </summary>
    public DateTime LastSyncedAt { get; set; }
}
