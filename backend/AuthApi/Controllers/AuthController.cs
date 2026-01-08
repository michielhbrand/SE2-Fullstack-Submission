using AuthApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IKeycloakAuthService _keycloakAuthService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IKeycloakAuthService keycloakAuthService,
        ILogger<AuthController> logger)
    {
        _keycloakAuthService = keycloakAuthService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required" });
        }

        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password);

        if (tokenResponse == null)
        {
            _logger.LogWarning("Login failed for user: {Username}", request.Username);
            return Unauthorized(new { error = "Invalid username or password" });
        }

        _logger.LogInformation("Login successful for user: {Username}", request.Username);

        return Ok(new
        {
            access_token = tokenResponse.AccessToken,
            refresh_token = tokenResponse.RefreshToken,
            expires_in = tokenResponse.ExpiresIn,
            token_type = tokenResponse.TokenType
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        _logger.LogInformation("Logout attempt");

        var success = await _keycloakAuthService.LogoutAsync(request.RefreshToken);

        if (!success)
        {
            _logger.LogWarning("Logout failed");
            return BadRequest(new { error = "Logout failed" });
        }

        _logger.LogInformation("Logout successful");

        return Ok(new { message = "Logout successful" });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
