using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.DTOs.User;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Services.User;

/// <summary>
/// Service for reading from the UserDirectory read model.
/// Provides fast, denormalized queries without runtime Keycloak calls.
/// </summary>
public class UserDirectoryService : IUserDirectoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IKeycloakAuthService _keycloakService;
    private readonly ILogger<UserDirectoryService> _logger;

    public UserDirectoryService(
        ApplicationDbContext context,
        IKeycloakAuthService keycloakService,
        ILogger<UserDirectoryService> logger)
    {
        _context = context;
        _keycloakService = keycloakService;
        _logger = logger;
    }

    public async Task<PagedUserDirectoryResponse> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.UserDirectory.AsQueryable();

        // Apply filters
        if (query.ActiveOnly.HasValue)
        {
            queryable = queryable.Where(u => u.Active == query.ActiveOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            queryable = queryable.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        // Get total count before pagination
        var totalCount = await queryable.CountAsync(cancellationToken);

        // Apply sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "firstname" => query.SortDescending
                ? queryable.OrderByDescending(u => u.FirstName)
                : queryable.OrderBy(u => u.FirstName),
            "lastname" => query.SortDescending
                ? queryable.OrderByDescending(u => u.LastName)
                : queryable.OrderBy(u => u.LastName),
            "createdat" => query.SortDescending
                ? queryable.OrderByDescending(u => u.CreatedAt)
                : queryable.OrderBy(u => u.CreatedAt),
            "active" => query.SortDescending
                ? queryable.OrderByDescending(u => u.Active)
                : queryable.OrderBy(u => u.Active),
            _ => query.SortDescending
                ? queryable.OrderByDescending(u => u.Email)
                : queryable.OrderBy(u => u.Email)
        };

        // Apply pagination and project to DTO
        var users = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Active = u.Active,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedUserDirectoryResponse
        {
            Users = users,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UserResponse> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserDirectory
            .Where(u => u.Id == userId)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Active = u.Active,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        return user;
    }

    public async Task SyncUserAsync(
        string userId,
        string adminToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user from app database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in app database, skipping sync", userId);
                return;
            }

            // Get all users from Keycloak to find this specific user
            var keycloakUsers = await _keycloakService.GetAllUsersAsync(adminToken);
            var keycloakUser = keycloakUsers.FirstOrDefault(u => u.Id == userId);

            if (keycloakUser == null)
            {
                _logger.LogWarning("User {UserId} not found in Keycloak, skipping sync", userId);
                return;
            }

            // Get roles as comma-separated string
            var roles = string.Join(",", keycloakUser.Roles);

            // Upsert to UserDirectory
            var userDirectory = await _context.UserDirectory
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (userDirectory == null)
            {
                userDirectory = new Models.UserDirectory
                {
                    Id = userId,
                    Email = keycloakUser.Email,
                    FirstName = keycloakUser.FirstName,
                    LastName = keycloakUser.LastName,
                    Roles = roles,
                    KeycloakEnabled = keycloakUser.Enabled,
                    Active = user.Active,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    LastSyncedAt = DateTime.UtcNow
                };
                _context.UserDirectory.Add(userDirectory);
            }
            else
            {
                userDirectory.Email = keycloakUser.Email;
                userDirectory.FirstName = keycloakUser.FirstName;
                userDirectory.LastName = keycloakUser.LastName;
                userDirectory.Roles = roles;
                userDirectory.KeycloakEnabled = keycloakUser.Enabled;
                userDirectory.Active = user.Active;
                userDirectory.CreatedAt = user.CreatedAt;
                userDirectory.UpdatedAt = user.UpdatedAt;
                userDirectory.LastSyncedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Synced user {UserId} to UserDirectory", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user {UserId} to UserDirectory", userId);
            throw;
        }
    }
}
