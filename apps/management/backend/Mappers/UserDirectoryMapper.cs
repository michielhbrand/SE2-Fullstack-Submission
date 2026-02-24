using ManagementApi.DTOs.User;
using Shared.Database.Models;

namespace ManagementApi.Mappers;

/// <summary>
/// Mapper for converting UserDirectory read model to DTOs
/// </summary>
public static class UserDirectoryMapper
{
    public static UserResponse ToResponse(this UserDirectory userDirectory)
    {
        return new UserResponse
        {
            Id = userDirectory.Id,
            Email = userDirectory.Email,
            FirstName = userDirectory.FirstName,
            LastName = userDirectory.LastName,
            Active = userDirectory.Active,
            Role = ParseUserRole(userDirectory.Roles),
            CreatedAt = userDirectory.CreatedAt,
            UpdatedAt = userDirectory.UpdatedAt
        };
    }

    public static OrganizationMemberResponse ToOrganizationMemberResponse(
        this UserDirectory userDirectory,
        OrganizationMember membership)
    {
        return new OrganizationMemberResponse
        {
            UserId = userDirectory.Id,
            Email = userDirectory.Email,
            FirstName = userDirectory.FirstName,
            LastName = userDirectory.LastName,
            Active = userDirectory.Active,
            Role = ParseUserRole(userDirectory.Roles)
            // JoinedAt removed - not in database schema
        };
    }

    /// <summary>
    /// Parses Keycloak roles string and extracts the valid application role.
    /// Keycloak returns roles like "default-roles-microservices,orgAdmin".
    /// We only care about: orgUser, orgAdmin, or systemAdmin (Keycloak stores them with lowercase first char).
    /// </summary>
    private static UserRole ParseUserRole(string? rolesString)
    {
        if (string.IsNullOrWhiteSpace(rolesString))
        {
            return UserRole.OrgUser; // Default role
        }

        var roles = rolesString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToArray();

        if (roles.Any(r => r.Equals("systemAdmin", StringComparison.OrdinalIgnoreCase)))
        {
            return UserRole.SystemAdmin;
        }

        if (roles.Any(r => r.Equals("orgAdmin", StringComparison.OrdinalIgnoreCase)))
        {
            return UserRole.OrgAdmin;
        }

        if (roles.Any(r => r.Equals("orgUser", StringComparison.OrdinalIgnoreCase)))
        {
            return UserRole.OrgUser;
        }

        return UserRole.OrgUser;
    }
}
