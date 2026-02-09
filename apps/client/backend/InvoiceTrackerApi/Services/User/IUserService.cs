using InvoiceTrackerApi.DTOs.Auth.Requests;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Services.Auth;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    Task<string> CreateUserAsync(string adminToken, CreateUserRequest request, CancellationToken cancellationToken = default);
    Task UpdateUserRoleAsync(string adminToken, string userId, UserRole role, CancellationToken cancellationToken = default);
}
