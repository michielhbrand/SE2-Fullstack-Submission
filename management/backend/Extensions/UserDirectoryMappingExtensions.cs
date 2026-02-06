using ManagementApi.DTOs.User;
using ManagementApi.Models;

namespace ManagementApi.Extensions;

/// <summary>
/// Mapping extensions for UserDirectory read model
/// </summary>
public static class UserDirectoryMappingExtensions
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
            Role = membership.Role,
            JoinedAt = membership.JoinedAt
        };
    }
}
