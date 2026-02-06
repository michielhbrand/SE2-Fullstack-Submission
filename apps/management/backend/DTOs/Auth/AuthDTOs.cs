namespace ManagementApi.DTOs.Auth;

public record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public required string TokenType { get; init; }
    public List<string> Roles { get; init; } = new();
}

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public record LogoutRequest
{
    public required string RefreshToken { get; init; }
}
