using InvoiceTrackerApi.DTOs.User;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service for reading from the UserDirectory read model.
/// This service provides fast, denormalized queries for UI tables.
/// Also handles syncing users from Keycloak to UserDirectory.
/// </summary>
public interface IUserDirectoryService
{    Task<PagedUserDirectoryResponse> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken = default);
    
    Task<UserResponse> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task SyncUserAsync(
        string userId,
        string adminToken,
        CancellationToken cancellationToken = default);
}
