namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Service interface for Keycloak authentication operations.
/// All methods throw exceptions on failure rather than returning null or false.
/// </summary>
public interface IKeycloakAuthService
{
    /// <summary>
    /// Authenticates a user and returns access tokens.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">When credentials are invalid</exception>
    /// <exception cref="Exceptions.UnauthorizedException">When authentication fails</exception>
    /// <exception cref="Exceptions.ForbiddenException">When admin login attempted without admin role</exception>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
    Task<TokenResponse> LoginAsync(string username, string password, bool isAdminLogin = false);
    
    /// <summary>
    /// Logs out a user by invalidating their refresh token.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">When refresh token is invalid</exception>
    /// <exception cref="Exceptions.BusinessRuleException">When logout fails</exception>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
    Task LogoutAsync(string refreshToken);
    
    /// <summary>
    /// Gets all users from Keycloak (admin only).
    /// </summary>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
    Task<List<UserInfo>> GetAllUsersAsync(string adminToken);
    
    /// <summary>
    /// Updates a user's role (admin only).
    /// </summary>
    /// <exception cref="Exceptions.ForbiddenException">When attempting self-demotion</exception>
    /// <exception cref="Exceptions.BusinessRuleException">When role update fails</exception>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
    Task UpdateUserRoleAsync(string adminToken, string userId, bool isAdmin);
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
