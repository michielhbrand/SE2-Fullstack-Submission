using ManagementApi.DTOs.Auth;

namespace ManagementApi.Services.Auth;

public interface IKeycloakTokenService
{
    Task<TokenResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default);
}
