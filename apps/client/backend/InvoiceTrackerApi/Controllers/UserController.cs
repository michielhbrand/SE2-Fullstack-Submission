using InvoiceTrackerApi.DTOs.Auth.Requests;
using InvoiceTrackerApi.DTOs.User;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Services.Auth;
using InvoiceTrackerApi.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Controller for user management operations following layered architecture
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : AuthenticatedControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        IUserDirectoryService userDirectoryService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _userDirectoryService = userDirectoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get users from UserDirectory with pagination and filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="searchTerm">Search term for email, firstname, or lastname</param>
    /// <param name="sortBy">Field to sort by (Email, FirstName, LastName, CreatedAt, Active)</param>
    /// <param name="sortDescending">Sort in descending order</param>
    /// <param name="activeOnly">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of users</returns>
    [HttpGet("directory")]
    [ProducesResponseType(typeof(PagedUserDirectoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserDirectory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = "Email",
        [FromQuery] bool sortDescending = false,
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new UserDirectoryQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDescending = sortDescending,
            ActiveOnly = activeOnly
        };

        var result = await _userDirectoryService.GetUsersAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a single user by ID from UserDirectory
    /// </summary>
    /// <param name="userId">User ID (Keycloak UUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserById(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userDirectoryService.GetUserByIdAsync(userId, cancellationToken);
        return Ok(user);
    }

    /// <summary>
    /// Create a new user (admin only)
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user ID</returns>
    [HttpPost]
    [Authorize(Roles = "orgAdmin,systemAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
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

        var userId = await _userService.CreateUserAsync(token, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { userId, message = "User created successfully" });
    }

    /// <summary>
    /// Update user role (admin only)
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">Role update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPut("{userId}/role")]
    [Authorize(Roles = "orgAdmin,systemAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(
        string userId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        await _userService.UpdateUserRoleAsync(token, userId, request.Role, cancellationToken);

        return Ok(new { message = "User role updated successfully" });
    }

    /// <summary>
    /// Update user details (admin only)
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">User details update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPut("{userId}")]
    [Authorize(Roles = "orgAdmin,systemAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserDetails(
        string userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        await _userService.UpdateUserDetailsAsync(token, userId, request, cancellationToken);

        return Ok(new { message = "User details updated successfully" });
    }
}
