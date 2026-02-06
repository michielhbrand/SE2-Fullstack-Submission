using ManagementApi.DTOs.User;

namespace ManagementApi.Services.User;

/// <summary>
/// Service for reading from the UserDirectory read model.
/// This service provides fast, denormalized queries for UI tables.
/// DO NOT use this service for write operations.
/// </summary>
public interface IUserDirectoryService
{
    /// <summary>
    /// Get all users from the UserDirectory with pagination, filtering, and sorting
    /// </summary>
    Task<PagedUserDirectoryResponse> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a single user from the UserDirectory by ID
    /// </summary>
    Task<UserResponse> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get organization members from the UserDirectory
    /// </summary>
    Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync(
        int organizationId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sync a single user from Keycloak to UserDirectory
    /// </summary>
    Task SyncUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sync all users from Keycloak to UserDirectory
    /// </summary>
    Task SyncAllUsersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Query parameters for UserDirectory
/// </summary>
public class UserDirectoryQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Email";
    public bool SortDescending { get; set; } = false;
    public bool? ActiveOnly { get; set; }
}

/// <summary>
/// Paged response for UserDirectory queries
/// </summary>
public class PagedUserDirectoryResponse
{
    public List<UserResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
