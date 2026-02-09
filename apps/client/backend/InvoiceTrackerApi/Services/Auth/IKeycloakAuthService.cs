namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Service interface for Keycloak authentication operations.
/// All methods throw exceptions on failure rather than returning null or false.
/// </summary>
public interface IKeycloakAuthService
{
    Task<TokenResponse> LoginAsync(string username, string password, bool isAdminLogin = false);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<List<UserInfo>> GetAllUsersAsync(string adminToken);
    Task UpdateUserRoleAsync(string adminToken, string userId, UserRole role);
    Task<string> CreateUserAsync(string adminToken, string username, string email, string firstName, string lastName, string password, UserRole role);
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public List<string> Roles { get; set; } = new();
}
