using InvoiceTrackerApi.DTOs.Auth.Requests;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Repositories.User;
using InvoiceTrackerApi.Services.Auth;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service for user management operations following layered architecture
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IKeycloakAuthService _keycloakService;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IKeycloakAuthService keycloakService,
        IUserDirectoryService userDirectoryService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _keycloakService = keycloakService;
        _userDirectoryService = userDirectoryService;
        _logger = logger;
    }

    public async Task<string> CreateUserAsync(
        string adminToken,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!UserRoleExtensions.TryParseRoleString(request.Role, out var role))
        {
            throw new Exceptions.ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
        }

        var userId = await _keycloakService.CreateUserAsync(
            adminToken,
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            role);

        var user = new Models.User
        {
            Id = userId,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        _logger.LogInformation("Created user {UserId} in app database", userId);

        try
        {
            await _userDirectoryService.SyncUserAsync(userId, adminToken, cancellationToken);
            _logger.LogInformation("Synced user {UserId} to UserDirectory", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync user {UserId} to UserDirectory", userId);
            // Don't fail the request if sync fails - it can be retried later
        }

        return userId;
    }

    public async Task UpdateUserRoleAsync(
        string adminToken,
        string userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        await _keycloakService.UpdateUserRoleAsync(adminToken, userId, role);

        try
        {
            await _userDirectoryService.SyncUserAsync(userId, adminToken, cancellationToken);
            _logger.LogInformation("Synced user {UserId} to UserDirectory after role update", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync user {UserId} to UserDirectory after role update", userId);
            // Don't fail the request if sync fails - it can be retried later
        }
    }
}
