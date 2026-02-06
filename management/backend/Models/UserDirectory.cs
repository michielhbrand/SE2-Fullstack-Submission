namespace ManagementApi.Models;

/// <summary>
/// UserDirectory is a read-optimized projection combining Keycloak identity data
/// with app database business/state data. This is a denormalized read model
/// that should NOT be used for writes.
/// 
/// This table is populated via an event-driven or periodic sync mechanism
/// and provides fast queries for UI tables without runtime Keycloak calls.
/// </summary>
public class UserDirectory
{
    /// <summary>
    /// Keycloak user ID (UUID string), used as primary key
    /// </summary>
    public required string Id { get; set; }
    
    // ===== Identity data from Keycloak (read-only in this model) =====
    
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
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
