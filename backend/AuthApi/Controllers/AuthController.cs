using AuthApi.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/auth")]
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

        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password, false);

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
            token_type = tokenResponse.TokenType,
            roles = tokenResponse.Roles
        });
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required" });
        }

        _logger.LogInformation("Admin login attempt for user: {Username}", request.Username);

        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password, true);

        if (tokenResponse == null)
        {
            _logger.LogWarning("Admin login failed for user: {Username}", request.Username);
            return Unauthorized(new { error = "Invalid credentials or insufficient permissions" });
        }

        _logger.LogInformation("Admin login successful for user: {Username}", request.Username);

        return Ok(new
        {
            access_token = tokenResponse.AccessToken,
            refresh_token = tokenResponse.RefreshToken,
            expires_in = tokenResponse.ExpiresIn,
            token_type = tokenResponse.TokenType,
            roles = tokenResponse.Roles
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

    [HttpGet("admin/users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Authorization token required" });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        _logger.LogInformation("Fetching all users");

        var users = await _keycloakAuthService.GetAllUsersAsync(token);

        return Ok(users);
    }

    [HttpPut("admin/users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Authorization token required" });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        _logger.LogInformation("Updating role for user: {UserId} to isAdmin: {IsAdmin}", userId, request.IsAdmin);

        var result = await _keycloakAuthService.UpdateUserRoleAsync(token, userId, request.IsAdmin);

        if (!result.Success)
        {
            _logger.LogWarning("Failed to update role for user: {UserId}. Reason: {Reason}", userId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage ?? "Failed to update user role" });
        }

        _logger.LogInformation("Successfully updated role for user: {UserId}", userId);

        return Ok(new { message = "User role updated successfully" });
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

public class UpdateRoleRequest
{
    public bool IsAdmin { get; set; }
}
