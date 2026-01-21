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
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">When refresh token is invalid</exception>
    /// <exception cref="Exceptions.UnauthorizedException">When refresh token is expired or invalid</exception>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    
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
    /// <param name="adminToken">Admin access token</param>
    /// <param name="userId">User ID to update</param>
    /// <param name="role">Role to assign.</param>
    /// <exception cref="Exceptions.ForbiddenException">When attempting self-demotion</exception>
    /// <exception cref="Exceptions.BusinessRuleException">When role update fails</exception>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
    Task UpdateUserRoleAsync(string adminToken, string userId, UserRole role);
    
    /// <summary>
    /// Creates a new user in Keycloak (admin only).
    /// </summary>
    /// <param name="adminToken">Admin access token</param>
    /// <param name="username">Username for the new user</param>
    /// <param name="email">Email address for the new user</param>
    /// <param name="firstName">First name of the new user</param>
    /// <param name="lastName">Last name of the new user</param>
    /// <param name="password">Initial password for the new user</param>
    /// <param name="role">Role to assign (orgUser or orgAdmin only)</param>
    /// <exception cref="Exceptions.ValidationException">When input is invalid</exception>
    /// <exception cref="Exceptions.ForbiddenException">When attempting to create systemAdmin</exception>
    /// <exception cref="Exceptions.ConflictException">When username or email already exists</exception>
    /// <exception cref="Exceptions.BusinessRuleException">When user creation fails</exception>
    /// <exception cref="Exceptions.InfrastructureException">When authentication service is unavailable</exception>
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
