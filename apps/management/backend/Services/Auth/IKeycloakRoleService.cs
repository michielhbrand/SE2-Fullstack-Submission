using Shared.Database.Models;

namespace ManagementApi.Services.Auth;

public interface IKeycloakRoleService
{
    Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserRoleAsync(string userId, UserRole newRole, CancellationToken cancellationToken = default);
    Task AssignRoleToUserAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task RemoveRolesFromUserAsync(string userId, List<string> roleNames, CancellationToken cancellationToken = default);
}
