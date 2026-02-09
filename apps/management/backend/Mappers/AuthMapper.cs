using ManagementApi.DTOs.Auth;
using ManagementApi.Services.Auth;

namespace ManagementApi.Mappers;

/// <summary>
/// Mapper for converting authentication-related objects to DTOs
/// </summary>
public static class AuthMapper
{
    /// <summary>
    /// Maps a TokenResponse from Keycloak to LoginResponse DTO
    /// </summary>
    /// <param name="result">The token response from Keycloak</param>
    /// <returns>LoginResponse DTO</returns>
    public static LoginResponse ToLoginResponse(this TokenResponse result)
    {
        return new()
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = result.TokenType,
            Roles = result.Roles
        };
    }
}
