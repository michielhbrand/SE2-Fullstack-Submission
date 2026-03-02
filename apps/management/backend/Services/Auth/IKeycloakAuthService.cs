using ManagementApi.DTOs.Auth;
using Shared.Core.Keycloak.Models;
using Shared.Database.Models;

namespace ManagementApi.Services.Auth;

public interface IKeycloakAuthService
{
    Task<TokenResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> CreateUserAsync(string email, string? firstName, string? lastName, string password, UserRole role, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(string userId, string? firstName, string? lastName, bool? enabled, CancellationToken cancellationToken = default);
    Task UpdateUserRoleAsync(string userId, UserRole newRole, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default);
}
