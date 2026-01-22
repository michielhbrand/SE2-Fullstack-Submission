using InvoiceTrackerApi.DTOs.Auth.Requests;
using InvoiceTrackerApi.DTOs.Auth.Responses;
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
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password, false);

        return Ok(new LoginResponse
        {
            Access_token = tokenResponse.AccessToken,
            Refresh_token = tokenResponse.RefreshToken,
            Expires_in = tokenResponse.ExpiresIn,
            Token_type = tokenResponse.TokenType,
            Roles = tokenResponse.Roles
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
    public async Task<ActionResult<LoginResponse>> AdminLogin([FromBody] LoginRequest request)
    {
        var tokenResponse = await _keycloakAuthService.LoginAsync(request.Username, request.Password, true);

        return Ok(new LoginResponse
        {
            Access_token = tokenResponse.AccessToken,
            Refresh_token = tokenResponse.RefreshToken,
            Expires_in = tokenResponse.ExpiresIn,
            Token_type = tokenResponse.TokenType,
            Roles = tokenResponse.Roles
        });
    }

    /// <summary>
    /// Refresh an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] LogoutRequest request)
    {
        var tokenResponse = await _keycloakAuthService.RefreshTokenAsync(request.RefreshToken);

        return Ok(new LoginResponse
        {
            Access_token = tokenResponse.AccessToken,
            Refresh_token = tokenResponse.RefreshToken,
            Expires_in = tokenResponse.ExpiresIn,
            Token_type = tokenResponse.TokenType,
            Roles = tokenResponse.Roles
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
    [Authorize(Roles = "orgAdmin,systemAdmin")] // UserRole.OrgAdmin, UserRole.SystemAdmin
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
    [Authorize(Roles = "orgAdmin,systemAdmin")] // UserRole.OrgAdmin, UserRole.SystemAdmin
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        await _keycloakAuthService.UpdateUserRoleAsync(token, userId, request.Role);

        return Ok(new { message = "User role updated successfully" });
    }

    /// <summary>
    /// Create a new user (admin only)
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user ID</returns>
    [HttpPost("admin/users")]
    [Authorize(Roles = "orgAdmin,systemAdmin")] // UserRole.OrgAdmin, UserRole.SystemAdmin
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        // Parse role string to UserRole enum
        if (!UserRoleExtensions.TryParseRoleString(request.Role, out var role))
        {
            return BadRequest(new { message = $"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}" });
        }

        // Additional protection: Ensure systemAdmin cannot be created
        if (role == UserRole.SystemAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Cannot create System Admin users through this endpoint" });
        }

        var userId = await _keycloakAuthService.CreateUserAsync(
            token,
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            role
        );

        return StatusCode(StatusCodes.Status201Created, new { userId, message = "User created successfully" });
    }
}
