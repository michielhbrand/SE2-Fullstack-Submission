namespace ManagementApi.DTOs.User;

/// <summary>
/// Response containing user information
/// </summary>
public class UserResponse
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response containing user information with organization memberships
/// </summary>
public class UserWithOrganizationsResponse
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Active { get; set; }
    public List<OrganizationMembershipResponse> Organizations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response containing organization membership information
/// </summary>
public class OrganizationMembershipResponse
{
    public int OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public required string Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// Response containing organization member information
/// </summary>
public class OrganizationMemberResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Active { get; set; }
    public required string Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
