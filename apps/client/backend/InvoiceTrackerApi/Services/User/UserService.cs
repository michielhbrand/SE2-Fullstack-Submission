using InvoiceTrackerApi.DTOs.Auth.Requests;
using Shared.Database.Models;
using InvoiceTrackerApi.Repositories.OrganizationMember;
using InvoiceTrackerApi.Repositories.User;
using InvoiceTrackerApi.Services.Auth;
using Shared.Core.Exceptions.Application;
using UserRole = InvoiceTrackerApi.Services.Auth.UserRole;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service for user management operations following layered architecture
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IKeycloakAuthService _keycloakService;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IKeycloakAuthService keycloakService,
        IUserDirectoryService userDirectoryService,
        IOrganizationMemberRepository organizationMemberRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _keycloakService = keycloakService;
        _userDirectoryService = userDirectoryService;
        _organizationMemberRepository = organizationMemberRepository;
        _logger = logger;
    }

    public async Task<string> CreateUserAsync(
        string adminToken,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!UserRoleExtensions.TryParseRoleString(request.Role, out var role))
        {
            throw new ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
        }

        var userId = await _keycloakService.CreateUserAsync(
            adminToken,
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            role);

        var user = new Shared.Database.Models.User
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

        // Add user as organization member if organizationId is provided
        if (request.OrganizationId.HasValue)
        {
            try
            {
                var member = new Shared.Database.Models.OrganizationMember
                {
                    OrganizationId = request.OrganizationId.Value,
                    UserId = userId,
                    Role = request.Role
                };

                await _organizationMemberRepository.AddMemberAsync(member);
                _logger.LogInformation(
                    "Added user {UserId} as {Role} to organization {OrganizationId}",
                    userId, request.Role, request.OrganizationId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to add user {UserId} to organization {OrganizationId}",
                    userId, request.OrganizationId.Value);
                // Don't fail the request - the user was created successfully
            }
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

    public async Task UpdateUserDetailsAsync(
        string adminToken,
        string userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Role != UserRole.OrgUser && request.Role != UserRole.OrgAdmin)
        {
            throw new ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
        }

        await _keycloakService.UpdateUserDetailsAsync(adminToken, userId, request.FirstName, request.LastName);
        await _keycloakService.UpdateUserRoleAsync(adminToken, userId, request.Role);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.Active = request.Active;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Updated user {UserId} active status to {Active}", userId, request.Active);
        }
        else
        {
            _logger.LogWarning("User {UserId} not found in app database, skipping Active status update", userId);
        }

        try
        {
            await _userDirectoryService.SyncUserAsync(userId, adminToken, cancellationToken);
            _logger.LogInformation("Synced user {UserId} to UserDirectory after details update", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync user {UserId} to UserDirectory after details update", userId);
            // Don't fail the request if sync fails - it can be retried later
        }
    }
}
