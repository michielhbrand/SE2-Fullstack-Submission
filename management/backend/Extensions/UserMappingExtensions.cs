using ManagementApi.DTOs.User;
using ManagementApi.Models;
using ManagementApi.Services.Auth;

namespace ManagementApi.Extensions;

/// <summary>
/// Mapping extensions for User entity (minimal write model)
/// Note: For read operations, use UserDirectoryMappingExtensions instead
/// </summary>
public static class UserMappingExtensions
{
    /// <summary>
    /// Converts User (write model) to UserResponse by fetching identity data from Keycloak
    /// This should only be used for write operations. For reads, use UserDirectory.
    /// </summary>
    public static async Task<UserResponse> ToResponseAsync(
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
