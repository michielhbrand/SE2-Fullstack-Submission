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
    public required string Role { get; set; } = "orgUser";
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
    public required string Role { get; set; } = "member"; // owner, admin, member
}

/// <summary>
/// Request to create a user and add them to an organization in one step
/// </summary>
public class CreateOrganizationMemberRequest
{
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Role { get; set; } = "orgUser";
}
