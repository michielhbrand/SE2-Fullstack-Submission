using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Controller for authentication and user management operations
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : AuthenticatedControllerBase
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

    /// <summary>
    /// Authenticate a user and return access tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Access and refresh tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Authenticate an admin user and return access tokens
    /// </summary>
    /// <param name="request">Admin login credentials</param>
    /// <returns>Access and refresh tokens</returns>
    [HttpPost("admin/login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Logout a user by invalidating their refresh token
    /// </summary>
    /// <param name="request">Logout request containing refresh token</param>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Get all users (admin only)
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet("admin/users")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        
        _logger.LogInformation("Fetching all users");

        var users = await _keycloakAuthService.GetAllUsersAsync(token);

        return Ok(users);
    }

    /// <summary>
    /// Update user role (admin only)
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">Role update request</param>
    /// <returns>Success message</returns>
    [HttpPut("admin/users/{userId}/role")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        
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
