using ManagementApi.Data;
using ManagementApi.DTOs.User;
using ManagementApi.Exceptions.Application;
using ManagementApi.Mappers;
using ManagementApi.Models;
using ManagementApi.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Services.User;

/// <summary>
/// Service for user write operations.
/// Writes to Keycloak for identity data and app DB for business/state data.
/// For read operations, use IUserDirectoryService instead.
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IKeycloakAuthService _keycloakService;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        IKeycloakAuthService keycloakService,
        IUserDirectoryService userDirectoryService,
        ILogger<UserService> logger)
    {
        _context = context;
        _keycloakService = keycloakService;
        _userDirectoryService = userDirectoryService;
        _logger = logger;
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user already exists in local database
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new ValidationException("A user with this email already exists");
        }

        // Create user in Keycloak
        var keycloakUser = await _keycloakService.CreateUserAsync(
            request.Email,
            request.FirstName,
            request.LastName,
            request.Email, // Default password is email
            request.Role,
            cancellationToken);

        // Create user in local database
        var user = new Models.User
        {
            Id = keycloakUser.Id,
            Active = request.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, request.Email);

        await _userDirectoryService.SyncUserAsync(user.Id, cancellationToken);

        return await _userDirectoryService.GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _userDirectoryService.GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<UserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var userDirectory = await _context.UserDirectory
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (userDirectory == null)
        {
            throw new NotFoundException($"User with email '{email}' not found");
        }

        return userDirectory.ToResponse();
    }

    public async Task<UserWithOrganizationsResponse> GetUserWithOrganizationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userDirectory = await _context.UserDirectory
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (userDirectory == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        var memberships = await _context.OrganizationMembers
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        var userResponse = userDirectory.ToResponse();

        return new UserWithOrganizationsResponse
        {
            Id = userDirectory.Id,
            Email = userDirectory.Email,
            FirstName = userDirectory.FirstName,
            LastName = userDirectory.LastName,
            Active = userDirectory.Active,
            Role = userResponse.Role,
            Organizations = memberships.Select(m => new OrganizationMembershipResponse
            {
                OrganizationId = m.OrganizationId,
                OrganizationName = m.Organization.Name,
                Role = userResponse.Role,
                JoinedAt = m.JoinedAt
            }).ToList(),
            CreatedAt = userDirectory.CreatedAt,
            UpdatedAt = userDirectory.UpdatedAt
        };
    }

    public async Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        // Update identity fields in Keycloak
        if (request.FirstName != null || request.LastName != null || request.Active.HasValue)
        {
            await _keycloakService.UpdateUserAsync(
                userId,
                request.FirstName,
                request.LastName,
                request.Active,
                cancellationToken);
        }

        // Update role in Keycloak if provided
        if (request.Role.HasValue)
        {
            await _keycloakService.UpdateUserRoleAsync(userId, request.Role.Value, cancellationToken);
            _logger.LogInformation("Updated role for user {UserId} to {Role}", userId, request.Role.Value);
        }

        // Update domain data in local database
        if (request.Active.HasValue)
        {
            user.Active = request.Active.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user {UserId}", userId);

        await _userDirectoryService.SyncUserAsync(userId, cancellationToken);

        return await _userDirectoryService.GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<List<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var query = new UserDirectoryQuery
        {
            Page = 1,
            PageSize = int.MaxValue // Get all users
        };

        var result = await _userDirectoryService.GetUsersAsync(query, cancellationToken);
        return result.Users;
    }

    public async Task<OrganizationMemberResponse> AddUserToOrganizationAsync(
        int organizationId,
        CreateOrganizationMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check if organization exists
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException($"Organization with ID '{organizationId}' not found");
        }

        // Try to find existing user by email in UserDirectory
        var userDirectory = await _context.UserDirectory
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        string userId;

        // If user doesn't exist, create them
        if (userDirectory == null)
        {
            var createUserRequest = new CreateUserRequest
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Active = true,
                Role = request.Role
            };

            var userResponse = await CreateUserAsync(createUserRequest, cancellationToken);
            userId = userResponse.Id;
            
            // Refresh userDirectory
            userDirectory = await _context.UserDirectory
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (userDirectory == null)
            {
                throw new ServiceUnavailableException("Failed to create user");
            }
        }
        else
        {
            userId = userDirectory.Id;
        }

        // Check if user is already a member of this organization
        var existingMembership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, cancellationToken);

        if (existingMembership != null)
        {
            throw new ValidationException("User is already a member of this organization");
        }

        // Add user to organization
        var membership = new OrganizationMember
        {
            OrganizationId = organizationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added user {UserId} to organization {OrganizationId}",
            userId, organizationId);

        return userDirectory.ToOrganizationMemberResponse(membership);
    }

    public async Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _userDirectoryService.GetOrganizationMembersAsync(organizationId, cancellationToken);
    }

    public async Task RemoveUserFromOrganizationAsync(
        int organizationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, cancellationToken);

        if (membership == null)
        {
            throw new NotFoundException("User is not a member of this organization");
        }

        _context.OrganizationMembers.Remove(membership);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed user {UserId} from organization {OrganizationId}", userId, organizationId);
    }
}
