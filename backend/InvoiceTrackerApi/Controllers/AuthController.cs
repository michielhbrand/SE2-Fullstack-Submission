using InvoiceTrackerApi.DTOs.Auth.Requests;
using InvoiceTrackerApi.DTOs.Auth.Responses;
using InvoiceTrackerApi.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Controller for authentication operations
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

}
