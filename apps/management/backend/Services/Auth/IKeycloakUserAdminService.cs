using Shared.Core.Keycloak.Models;
using Shared.Database.Models;

namespace ManagementApi.Services.Auth;

public interface IKeycloakUserAdminService
{
    Task<KeycloakUserResponse> CreateUserAsync(string email, string? firstName, string? lastName, string password, UserRole role, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(string userId, string? firstName, string? lastName, bool? enabled, CancellationToken cancellationToken = default);
}
