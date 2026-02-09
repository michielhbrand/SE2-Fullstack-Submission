using ManagementApi.Models;

namespace ManagementApi.DTOs.User;

/// <summary>
/// Request to create a new user
/// </summary>
public class CreateUserRequest
{
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Active { get; set; } = true;
    public required UserRole Role { get; set; } = UserRole.OrgUser;
}

/// <summary>
/// Request to update an existing user
/// </summary>
public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? Active { get; set; }
}

/// <summary>
/// Request to add a user to an organization
/// </summary>
public class AddUserToOrganizationRequest
{
    public required string UserId { get; set; }
    public required int OrganizationId { get; set; }
    public required UserRole Role { get; set; } = UserRole.OrgUser;
}

/// <summary>
/// Request to create a user and add them to an organization in one step
/// </summary>
public class CreateOrganizationMemberRequest
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required UserRole Role { get; set; } = UserRole.OrgUser;
}

/// <summary>
/// Request to get user directory with pagination and filtering
/// </summary>
public class GetUserDirectoryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Email";
    public bool SortDescending { get; set; } = false;
    public bool? ActiveOnly { get; set; }
}

/// <summary>
/// Request to get a user by ID
/// </summary>
public class GetUserRequest
{
    public required string UserId { get; set; }
}

/// <summary>
/// Request to get organization members
/// </summary>
public class GetOrganizationMembersRequest
{
    public int OrganizationId { get; set; }
}

/// <summary>
/// Request to remove a user from an organization
/// </summary>
public class RemoveUserFromOrganizationRequest
{
    public int OrganizationId { get; set; }
    public required string UserId { get; set; }
}
