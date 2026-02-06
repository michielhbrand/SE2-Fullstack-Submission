using System.Runtime.Serialization;

namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Defines the available user roles in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular organization user with basic permissions
    /// </summary>
    [EnumMember(Value = "orgUser")]
    OrgUser,
    
    /// <summary>
    /// Organization administrator with elevated permissions
    /// </summary>
    [EnumMember(Value = "orgAdmin")]
    OrgAdmin,
    
    /// <summary>
    /// System administrator with full permissions
    /// </summary>
    [EnumMember(Value = "systemAdmin")]
    SystemAdmin
}

/// <summary>
/// Extension methods for UserRole enum
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Converts the enum value to its Keycloak role string representation
    /// </summary>
    public static string ToRoleString(this UserRole role)
    {
        return role switch
        {
            UserRole.OrgUser => "orgUser",
            UserRole.OrgAdmin => "orgAdmin",
            UserRole.SystemAdmin => "systemAdmin",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid user role")
        };
    }
    
    /// <summary>
    /// Parses a role string to its corresponding UserRole enum value
    /// </summary>
    public static UserRole FromRoleString(string roleString)
    {
        return roleString?.ToLowerInvariant() switch
        {
            "orguser" => UserRole.OrgUser,
            "orgadmin" => UserRole.OrgAdmin,
            "systemadmin" => UserRole.SystemAdmin,
            _ => throw new ArgumentException($"Invalid role string: {roleString}", nameof(roleString))
        };
    }
    
    /// <summary>
    /// Tries to parse a role string to its corresponding UserRole enum value
    /// </summary>
    public static bool TryParseRoleString(string? roleString, out UserRole role)
    {
        role = UserRole.OrgUser;
        
        if (string.IsNullOrWhiteSpace(roleString))
            return false;
            
        try
        {
            role = FromRoleString(roleString);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets all valid role strings
    /// </summary>
    public static string[] GetAllRoleStrings()
    {
        return new[]
        {
            UserRole.OrgUser.ToRoleString(),
            UserRole.OrgAdmin.ToRoleString(),
            UserRole.SystemAdmin.ToRoleString()
        };
    }
    
    /// <summary>
    /// Gets all assignable role strings (excludes SystemAdmin)
    /// </summary>
    public static string[] GetAssignableRoleStrings()
    {
        return new[]
        {
            UserRole.OrgUser.ToRoleString(),
            UserRole.OrgAdmin.ToRoleString()
        };
    }
}
