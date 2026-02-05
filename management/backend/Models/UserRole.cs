using System.Text.Json.Serialization;

namespace ManagementApi.Models;

/// <summary>
/// Defines the allowed roles for users within an organization.
/// This is the single source of truth for user roles in the system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// Regular organization user with basic permissions
    /// </summary>
    OrgUser,
    
    /// <summary>
    /// Organization administrator with elevated permissions
    /// </summary>
    OrgAdmin
}
