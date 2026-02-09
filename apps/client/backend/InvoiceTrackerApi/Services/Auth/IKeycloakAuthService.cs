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
    Task UpdateUserDetailsAsync(string adminToken, string userId, string firstName, string lastName);
}
