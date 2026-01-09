namespace AuthApi.Services;

public interface IKeycloakAuthService
{
    Task<TokenResponse?> LoginAsync(string username, string password, bool isAdminLogin = false);
    Task<bool> LogoutAsync(string refreshToken);
    Task<List<UserInfo>> GetAllUsersAsync(string adminToken);
    Task<UpdateRoleResult> UpdateUserRoleAsync(string adminToken, string userId, bool isAdmin);
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

public class UpdateRoleResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static UpdateRoleResult Successful() => new() { Success = true };
    public static UpdateRoleResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
