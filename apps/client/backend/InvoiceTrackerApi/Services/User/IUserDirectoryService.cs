using InvoiceTrackerApi.DTOs.User;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service for reading from the UserDirectory read model.
/// This service provides fast, denormalized queries for UI tables.
/// Also handles syncing users from Keycloak to UserDirectory.
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
    /// Sync a single user from Keycloak to UserDirectory
    /// </summary>
    Task SyncUserAsync(
        string userId,
        string adminToken,
        CancellationToken cancellationToken = default);
}
