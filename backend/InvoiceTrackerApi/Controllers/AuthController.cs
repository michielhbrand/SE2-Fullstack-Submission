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
        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password, false);

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
        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password, true);

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
        await _keycloakAuthService.LogoutAsync(request.RefreshToken);

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

        await _keycloakAuthService.UpdateUserRoleAsync(token, userId, request.IsAdmin);

        return Ok(new { message = "User role updated successfully" });
    }
}
