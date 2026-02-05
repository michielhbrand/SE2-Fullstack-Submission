using ManagementApi.DTOs.User;
using ManagementApi.Models;

namespace ManagementApi.Extensions;

public static class UserMappingExtensions
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Active = user.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public static UserWithOrganizationsResponse ToResponseWithOrganizations(this User user)
    {
        return new UserWithOrganizationsResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Active = user.Active,
            Organizations = user.OrganizationMemberships.Select(m => new OrganizationMembershipResponse
            {
                OrganizationId = m.OrganizationId,
                OrganizationName = m.Organization.Name,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public static OrganizationMemberResponse ToOrganizationMemberResponse(this User user, OrganizationMember membership)
    {
        return new OrganizationMemberResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Active = user.Active,
            Role = membership.Role,
            JoinedAt = membership.JoinedAt
        };
    }
}
