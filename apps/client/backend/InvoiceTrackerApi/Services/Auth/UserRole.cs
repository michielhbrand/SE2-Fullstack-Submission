using System.Runtime.Serialization;

namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Defines the available user roles in the system
/// </summary>
public enum UserRole
{
    [EnumMember(Value = "orgUser")]
    OrgUser,

    [EnumMember(Value = "orgAdmin")]
    OrgAdmin,

    [EnumMember(Value = "systemAdmin")]
    SystemAdmin
}

/// <summary>
/// Extension methods for UserRole enum
/// </summary>
public static class UserRoleExtensions
{
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

    public static string[] GetAllRoleStrings()
    {
        return new[]
        {
            UserRole.OrgUser.ToRoleString(),
            UserRole.OrgAdmin.ToRoleString(),
            UserRole.SystemAdmin.ToRoleString()
        };
    }

    public static string[] GetAssignableRoleStrings()
    {
        return new[]
        {
            UserRole.OrgUser.ToRoleString(),
            UserRole.OrgAdmin.ToRoleString()
        };
    }
}
