using ManagementApi.Data;
using ManagementApi.DTOs.User;
using ManagementApi.Exceptions.Application;
using ManagementApi.Extensions;
using ManagementApi.Models;
using ManagementApi.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Services.User;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IKeycloakAuthService _keycloakService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        IKeycloakAuthService keycloakService,
        ILogger<UserService> logger)
    {
        _context = context;
        _keycloakService = keycloakService;
        _logger = logger;
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user already exists in local database
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (existingUser != null)
        {
            throw new ValidationException("A user with this email already exists");
        }

        // Create user in Keycloak with email as default password
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
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Active = request.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);

        return user.ToResponse();
    }

    public async Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        return user.ToResponse();
    }

    public async Task<UserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with email '{email}' not found");
        }

        return user.ToResponse();
    }

    public async Task<UserWithOrganizationsResponse> GetUserWithOrganizationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.OrganizationMemberships)
            .ThenInclude(m => m.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        return user.ToResponseWithOrganizations();
    }

    public async Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found");
        }

        // Update in Keycloak
        await _keycloakService.UpdateUserAsync(
            userId,
            request.FirstName,
            request.LastName,
            request.Active,
            cancellationToken);

        // Update in local database
        if (request.FirstName != null)
            user.FirstName = request.FirstName;

        if (request.LastName != null)
            user.LastName = request.LastName;

        if (request.Active.HasValue)
            user.Active = request.Active.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user {UserId}", userId);

        return user.ToResponse();
    }

    public async Task<List<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        return users.Select(u => u.ToResponse()).ToList();
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

        // Try to find existing user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        // If user doesn't exist, create them
        if (user == null)
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
            
            // Fetch the created user
            user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userResponse.Id, cancellationToken);

            if (user == null)
            {
                throw new ServiceUnavailableException("Failed to create user");
            }
        }

        // Check if user is already a member of this organization
        var existingMembership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == user.Id, cancellationToken);

        if (existingMembership != null)
        {
            throw new ValidationException("User is already a member of this organization");
        }

        // Add user to organization
        var membership = new OrganizationMember
        {
            OrganizationId = organizationId,
            UserId = user.Id,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow
        };

        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added user {UserId} to organization {OrganizationId} with role {Role}",
            user.Id, organizationId, request.Role);

        return user.ToOrganizationMemberResponse(membership);
    }

    public async Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var members = await _context.OrganizationMembers
            .Include(m => m.Organization)
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        var memberResponses = new List<OrganizationMemberResponse>();

        foreach (var member in members)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);

            if (user != null)
            {
                memberResponses.Add(user.ToOrganizationMemberResponse(member));
            }
        }

        return memberResponses.OrderBy(m => m.Email).ToList();
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
