using ManagementApi.DTOs.User;
using Shared.Core.Keycloak.Models;
using Shared.Database.Models;

namespace ManagementApi.Mappers;

/// <summary>
/// Mapper for converting User entity (minimal write model) to DTOs
/// Note: For read operations, use UserDirectoryMapper instead
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Converts User (write model) to UserResponse by fetching identity data from Keycloak
    /// This should only be used for write operations. For reads, use UserDirectory.
    /// </summary>
    public static UserResponse ToResponseAsync(
        this User user,
        KeycloakUserResponse keycloakUser)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = keycloakUser.Email,
            FirstName = keycloakUser.FirstName,
            LastName = keycloakUser.LastName,
            Active = user.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
