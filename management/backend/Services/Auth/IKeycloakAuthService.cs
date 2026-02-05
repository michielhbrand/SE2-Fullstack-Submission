using ManagementApi.Models;

namespace ManagementApi.Services.Auth;

public interface IKeycloakAuthService
{
    Task<TokenResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    // User management methods
    Task<KeycloakUserResponse> CreateUserAsync(string email, string? firstName, string? lastName, string password, UserRole role, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<KeycloakUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(string userId, string? firstName, string? lastName, bool? enabled, CancellationToken cancellationToken = default);
    Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default);
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class KeycloakUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public long CreatedTimestamp { get; set; }
}
