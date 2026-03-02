using Shared.Database.Data;
using ManagementApi.DTOs.User;
using Shared.Core.Exceptions.Application;
using ManagementApi.Mappers;
using Shared.Database.Models;
using ManagementApi.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Services.User;

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

        // Apply pagination
        var users = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedUserDirectoryResponse
        {
            Users = users.Select(u => u.ToResponse()).ToList(),
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
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        return user.ToResponse();
    }

    public async Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var members = await _context.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        var memberResponses = new List<OrganizationMemberResponse>();

        foreach (var member in members)
        {
            var userDirectory = await _context.UserDirectory
                .FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);

            if (userDirectory != null)
            {
                memberResponses.Add(userDirectory.ToOrganizationMemberResponse(member));
            }
        }

        return memberResponses.OrderBy(m => m.Email).ToList();
    }

    public async Task SyncUserAsync(string userId, CancellationToken cancellationToken = default)
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

            // Get user from Keycloak
            var keycloakUser = await _keycloakService.GetUserByIdAsync(userId, cancellationToken);

            // Get roles from Keycloak
            var rolesList = await _keycloakService.GetUserRolesAsync(userId, cancellationToken);
            var roles = string.Join(",", rolesList);

            // Upsert to UserDirectory
            var userDirectory = await _context.UserDirectory
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (userDirectory == null)
            {
                userDirectory = new UserDirectory
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

    public async Task SyncAllUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full sync of all users to UserDirectory");

        var users = await _context.Users.ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            try
            {
                await SyncUserAsync(user.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user {UserId}, continuing with next user", user.Id);
            }
        }

        _logger.LogInformation("Completed full sync of {Count} users to UserDirectory", users.Count);
    }
}
