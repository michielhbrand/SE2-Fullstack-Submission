using ManagementApi.DTOs.User;
using ManagementApi.Models;

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
