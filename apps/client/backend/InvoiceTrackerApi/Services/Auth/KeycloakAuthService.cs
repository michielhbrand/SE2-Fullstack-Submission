using System.Text.Json;
using InvoiceTrackerApi.Repositories.OrganizationMember;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using Shared.Core.Exceptions.Infrastructure;
using Shared.Core.Keycloak;
using Shared.Core.Keycloak.Models;

namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Main service for Keycloak authentication operations.
/// Orchestrates authentication, token management, and user operations.
/// </summary>
public class KeycloakAuthService : IKeycloakAuthService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakConfiguration _config;
    private readonly KeycloakAdminClient _adminClient;
    private readonly TokenService _tokenService;
    private readonly ILogger<KeycloakAuthService> _logger;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;

    public KeycloakAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakAuthService> logger,
        IOrganizationMemberRepository organizationMemberRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _organizationMemberRepository = organizationMemberRepository;
        
        _config = new KeycloakConfiguration(configuration, logger);
        
        _tokenService = new TokenService(new Logger<TokenService>(new LoggerFactory()));
        _adminClient = new KeycloakAdminClient(
            httpClient, 
            _config, 
            new Logger<KeycloakAdminClient>(new LoggerFactory()));
    }

    public async Task<TokenResponse> LoginAsync(string username, string password, bool isAdminLogin = false)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Username and password are required");
        }

        try
        {
            var clientId = isAdminLogin ? _config.AdminClientId : _config.ClientId;
            
            var requestData = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "grant_type", "password" },
                { "username", username },
                { "password", password }
            };

            var content = new FormUrlEncodedContent(requestData);
            
            _logger.LogInformation("Attempting {LoginType} login for user: {Username}",
                isAdminLogin ? "admin" : "normal", username);
            
            var response = await _httpClient.PostAsync(_config.GetTokenEndpoint(), content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for user: {Username}. Status: {Status}", username, response.StatusCode);
                throw new UnauthorizedException("Invalid username or password");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keycloakResponse == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                throw new InfrastructureException("Authentication service returned invalid response");
            }

            var roles = _tokenService.ExtractRolesFromToken(keycloakResponse.Access_Token);
            var userId = _tokenService.ExtractUserIdFromToken(keycloakResponse.Access_Token);
            
            // Validate that user belongs to at least one organization
            if (!string.IsNullOrEmpty(userId))
            {
                var belongsToOrganization = await _organizationMemberRepository.BelongsToAnyOrganizationAsync(userId);
                if (!belongsToOrganization)
                {
                    _logger.LogWarning("User {Username} (ID: {UserId}) attempted to login but does not belong to any organization", username, userId);
                    throw new ForbiddenException("User has not been assigned to an organization");
                }
            }
            else
            {
                _logger.LogError("Failed to extract user ID from token for user: {Username}", username);
                throw new InfrastructureException("Failed to validate user information");
            }
            
            // If admin login, verify user has orgAdmin or systemAdmin role
            if (isAdminLogin && !roles.Contains(UserRole.OrgAdmin.ToRoleString()) && !roles.Contains(UserRole.SystemAdmin.ToRoleString()))
            {
                _logger.LogWarning("User {Username} attempted admin login without admin role", username);
                throw new ForbiddenException("User does not have admin privileges");
            }

            _logger.LogInformation("Login successful for user: {Username} with roles: {Roles}", username, string.Join(", ", roles));

            return new TokenResponse
            {
                AccessToken = keycloakResponse.Access_Token,
                RefreshToken = keycloakResponse.Refresh_Token,
                ExpiresIn = keycloakResponse.Expires_In,
                TokenType = keycloakResponse.Token_Type,
                Roles = roles
            };
        }
        catch (AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for user: {Username}", username);
            throw new InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user: {Username}", username);
            throw new InfrastructureException("An unexpected error occurred during authentication", ex);
        }
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("Refresh token is required");
        }

        try
        {
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            var content = new FormUrlEncodedContent(requestData);
            
            _logger.LogInformation("Attempting to refresh token");
            
            var response = await _httpClient.PostAsync(_config.GetTokenEndpoint(), content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Token refresh failed. Status: {Status}", response.StatusCode);
                throw new UnauthorizedException("Refresh token is invalid or expired");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keycloakResponse == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                throw new InfrastructureException("Authentication service returned invalid response");
            }

            var roles = _tokenService.ExtractRolesFromToken(keycloakResponse.Access_Token);
            
            _logger.LogInformation("Token refresh successful with roles: {Roles}", string.Join(", ", roles));

            return new TokenResponse
            {
                AccessToken = keycloakResponse.Access_Token,
                RefreshToken = keycloakResponse.Refresh_Token,
                ExpiresIn = keycloakResponse.Expires_In,
                TokenType = keycloakResponse.Token_Type,
                Roles = roles
            };
        }
        catch (AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during token refresh");
            throw new InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            throw new InfrastructureException("An unexpected error occurred during token refresh", ex);
        }
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("Refresh token is required");
        }

        try
        {
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "refresh_token", refreshToken }
            };

            var content = new FormUrlEncodedContent(requestData);
            
            _logger.LogInformation("Attempting logout");
            
            var response = await _httpClient.PostAsync(_config.GetLogoutEndpoint(), content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Logout failed. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                
                // If the token is already invalid/expired, treat it as a successful logout
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogInformation("Refresh token already invalid/expired - treating as successful logout");
                    return;
                }
                
                throw new BusinessRuleException("Unable to logout. The refresh token may be invalid or expired.");
            }

            _logger.LogInformation("Logout successful");
        }
        catch (AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during logout");
            throw new InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            throw new InfrastructureException("An unexpected error occurred during logout", ex);
        }
    }

    public async Task<List<UserInfo>> GetAllUsersAsync(string adminToken)
    {
        try
        {
            var keycloakAdminToken = await _adminClient.GetAdminAccessTokenAsync();
            
            var keycloakUsers = await _adminClient.GetAllUsersAsync(keycloakAdminToken);
            
            var users = new List<UserInfo>();
            foreach (var user in keycloakUsers)
            {
                if (string.IsNullOrWhiteSpace(user.Username) ||
                    user.Username.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var roles = await _adminClient.GetUserRolesAsync(keycloakAdminToken, user.Id);
                
                // Only include users that have at least one of our application roles
                var appRoles = UserRoleExtensions.GetAllRoleStrings();
                if (!roles.Any(r => appRoles.Contains(r)))
                {
                    continue;
                }
                
                users.Add(new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Enabled = user.Enabled,
                    Roles = roles
                });
            }
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return new List<UserInfo>();
        }
    }

    public async Task UpdateUserRoleAsync(string adminToken, string userId, UserRole role)
    {
        try
        {
            // Prevent assignment of systemAdmin role
            if (role == UserRole.SystemAdmin)
            {
                _logger.LogWarning("Attempt to assign systemAdmin role to user {UserId} was blocked", userId);
                throw new ForbiddenException("Cannot assign System Admin role through this endpoint. System Admin role can only be managed directly in Keycloak.");
            }

            if (role != UserRole.OrgUser && role != UserRole.OrgAdmin)
            {
                throw new ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
            }

            var roleString = role.ToRoleString();

            var currentUserId = _tokenService.ExtractUserIdFromToken(adminToken);
            
            var keycloakAdminToken = await _adminClient.GetAdminAccessTokenAsync();
            
            var currentUserRoles = await _adminClient.GetUserRolesAsync(keycloakAdminToken, currentUserId ?? string.Empty);
            var isCurrentUserAdmin = currentUserRoles.Contains(UserRole.OrgAdmin.ToRoleString()) || currentUserRoles.Contains(UserRole.SystemAdmin.ToRoleString());
            
            if (!string.IsNullOrEmpty(currentUserId) && currentUserId == userId && role == UserRole.OrgUser && isCurrentUserAdmin)
            {
                _logger.LogWarning("User {UserId} attempted to demote themselves, which is not allowed", userId);
                throw new ForbiddenException("You cannot demote yourself");
            }

            var allRoles = await _adminClient.GetAllRolesAsync(keycloakAdminToken);
            
            var targetRole = allRoles.FirstOrDefault(r => r.Name.Equals(roleString, StringComparison.OrdinalIgnoreCase));
            
            if (targetRole == null)
            {
                _logger.LogWarning("Role {Role} not found in available roles", roleString);
                throw new BusinessRuleException($"Role '{roleString}' not found in realm");
            }
            
            _logger.LogInformation("Found role {Role} with ID: {RoleId}", roleString, targetRole.Id);
            
            var currentRoles = await _adminClient.GetUserRolesAsync(keycloakAdminToken, userId);
            var validRoles = UserRoleExtensions.GetAssignableRoleStrings();
            var rolesToRemove = allRoles
                .Where(r => currentRoles.Contains(r.Name) && validRoles.Contains(r.Name))
                .ToList();
            
            await _adminClient.RemoveUserRolesAsync(keycloakAdminToken, userId, rolesToRemove);
            
            await _adminClient.AddUserRoleAsync(keycloakAdminToken, userId, targetRole);
            
            _logger.LogInformation("Successfully updated user {UserId} to role {Role}", userId, roleString);
        }
        catch (AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error updating user role");
            throw new InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user role");
            throw new InfrastructureException("An unexpected error occurred while updating user role", ex);
        }
    }

    public async Task<string> CreateUserAsync(string adminToken, string username, string email, string firstName, string lastName, string password, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException("Username is required");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required");
        }
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationException("First name is required");
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationException("Last name is required");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required");
        }

        // Prevent creation of systemAdmin users
        if (role == UserRole.SystemAdmin)
        {
            _logger.LogWarning("Attempt to create systemAdmin user '{Username}' was blocked", username);
            throw new ForbiddenException("Cannot create System Admin users through this endpoint. System Admin role can only be managed directly in Keycloak.");
        }

        if (role != UserRole.OrgUser && role != UserRole.OrgAdmin)
        {
            throw new ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
        }

        try
        {
            var keycloakAdminToken = await _adminClient.GetAdminAccessTokenAsync();
            
            _logger.LogInformation("Creating new user: {Username} with role: {Role}", username, role.ToRoleString());
            
            var userId = await _adminClient.CreateUserAsync(keycloakAdminToken, username, email, firstName, lastName, password);
            
            _logger.LogInformation("User {Username} created successfully with ID: {UserId}", username, userId);
            
            await UpdateUserRoleAsync(adminToken, userId, role);
            
            _logger.LogInformation("Successfully created user {Username} with role {Role}", username, role.ToRoleString());
            
            return userId;
        }
        catch (AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating user {Username}", username);
            throw new InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating user {Username}", username);
            throw new InfrastructureException("An unexpected error occurred while creating user", ex);
        }
    }

    public async Task UpdateUserDetailsAsync(string adminToken, string userId, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ValidationException("User ID is required");
        }
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationException("First name is required");
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationException("Last name is required");
        }

        try
        {
            var keycloakAdminToken = await _adminClient.GetAdminAccessTokenAsync();
            
            _logger.LogInformation("Updating user details for user {UserId}", userId);
            
            await _adminClient.UpdateUserDetailsAsync(keycloakAdminToken, userId, firstName, lastName);
            
            _logger.LogInformation("Successfully updated user details for user {UserId}", userId);
        }
        catch (AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error updating user details for user {UserId}", userId);
            throw new InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user details for user {UserId}", userId);
            throw new InfrastructureException("An unexpected error occurred while updating user details", ex);
        }
    }
}
