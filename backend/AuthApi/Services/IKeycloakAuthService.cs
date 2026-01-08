namespace AuthApi.Services;

public interface IKeycloakAuthService
{
    Task<TokenResponse?> LoginAsync(string username, string password);
    Task<bool> LogoutAsync(string refreshToken);
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}
