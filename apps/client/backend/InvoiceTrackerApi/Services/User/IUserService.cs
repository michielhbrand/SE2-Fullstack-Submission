using InvoiceTrackerApi.DTOs.Auth.Requests;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Services.Auth;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Create a new user in Keycloak and app database, then sync to UserDirectory
    /// </summary>
    Task<string> CreateUserAsync(string adminToken, CreateUserRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update a user's role in Keycloak and sync to UserDirectory
    /// </summary>
    Task UpdateUserRoleAsync(string adminToken, string userId, UserRole role, CancellationToken cancellationToken = default);
}
