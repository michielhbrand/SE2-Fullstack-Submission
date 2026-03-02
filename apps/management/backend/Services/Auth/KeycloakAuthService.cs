using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using ManagementApi.DTOs.Auth;
using Shared.Core.Keycloak.Models;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using Shared.Database.Models;

namespace ManagementApi.Services.Auth;

public class KeycloakAuthService : IKeycloakAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAuthService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _adminUsername;
    private readonly string _adminPassword;

    public KeycloakAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var authority = _configuration["Keycloak:Authority"] 
            ?? throw new InvalidOperationException("Keycloak:Authority not configured");
        
        var authorityUri = new Uri(authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        _keycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        _realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";
        _clientId = _configuration["Keycloak:ClientId"] ?? "management-portal";
        _adminUsername = _configuration["Keycloak:AdminUsername"] ?? "admin";
        _adminPassword = _configuration["Keycloak:AdminPassword"] ?? "admin";
        
        _logger.LogInformation("KeycloakAuthService initialized - URL: {Url}, Realm: {Realm}, ClientId: {ClientId}",
            _keycloakUrl, _realm, _clientId);
    }

    public async Task<TokenResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Username and password are required");
        }

        try
        {
            var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";
            
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "grant_type", "password" },
                { "username", username },
                { "password", password }
            };

            var content = new FormUrlEncodedContent(requestData);
            
            _logger.LogInformation("Attempting systemAdmin login for user: {Username}", username);
            
            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed for user: {Username}. Status: {Status}",
                    username, response.StatusCode);
                throw new UnauthorizedException("Invalid username or password");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResponse == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                throw new ServiceUnavailableException("Authentication service returned invalid response");
            }

            var roles = ExtractRolesFromToken(keycloakResponse.Access_Token);
            
            // Verify user has systemAdmin role
            if (!roles.Contains("systemAdmin"))
            {
                _logger.LogWarning("User {Username} attempted login without systemAdmin role", username);
                throw new ForbiddenException("Access denied. System Administrator role required.");
            }

            _logger.LogInformation("Login successful for systemAdmin: {Username}", username);

            return new TokenResponse
            {
                AccessToken = keycloakResponse.Access_Token,
                RefreshToken = keycloakResponse.Refresh_Token,
                ExpiresIn = keycloakResponse.Expires_In,
                TokenType = keycloakResponse.Token_Type,
                Roles = roles
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for user: {Username}", username);
            throw new ServiceUnavailableException("Authentication service is unavailable");
        }
        catch (AppException)
        {
            // Re-throw application exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user: {Username}", username);
            throw;
        }
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("Refresh token is required");
        }

        try
        {
            var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";
            
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed. Status: {Status}", response.StatusCode);
                throw new UnauthorizedException("Refresh token is invalid or expired");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResponse == null)
            {
                throw new ServiceUnavailableException("Authentication service returned invalid response");
            }

            var roles = ExtractRolesFromToken(keycloakResponse.Access_Token);

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
            // Re-throw application exceptions as-is
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during token refresh");
            throw new ServiceUnavailableException("Authentication service is unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException("Refresh token is required");
        }

        try
        {
            var logoutEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/logout";
            
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "refresh_token", refreshToken }
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(logoutEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Logout failed. Status: {Status}", response.StatusCode);
            }

            _logger.LogInformation("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Don't throw - logout should be best effort
        }
    }

    private List<string> ExtractRolesFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var realmAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "realm_access");
            if (realmAccessClaim != null)
            {
                var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
                if (realmAccess.TryGetProperty("roles", out var rolesElement))
                {
                    return rolesElement.EnumerateArray()
                        .Select(r => r.GetString() ?? string.Empty)
                        .Where(r => !string.IsNullOrEmpty(r))
                        .ToList();
                }
            }
            
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting roles from token");
            return new List<string>();
        }
    }

    public async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenEndpoint = $"{_keycloakUrl}/realms/master/protocol/openid-connect/token";
            
            var requestData = new Dictionary<string, string>
            {
                { "client_id", "admin-cli" },
                { "grant_type", "password" },
                { "username", _adminUsername },
                { "password", _adminPassword }
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get admin access token. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to authenticate with Keycloak admin API");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Access_Token))
            {
                throw new ServiceUnavailableException("Invalid admin token response");
            }

            return tokenResponse.Access_Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin access token");
            throw;
        }
    }

    public async Task<KeycloakUserResponse> CreateUserAsync(string email, string? firstName, string? lastName, string password, UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
            var usersEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users";

            // Convert enum to lowercase string for Keycloak (e.g., OrgUser -> orgUser)
            var roleString = role.ToString();
            roleString = char.ToLowerInvariant(roleString[0]) + roleString.Substring(1);

            var userPayload = new
            {
                username = email,
                email = email,
                firstName = firstName ?? string.Empty,
                lastName = lastName ?? string.Empty,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
                // Note: realmRoles in user creation payload doesn't work reliably
                // We need to assign roles separately after user creation
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(userPayload),
                System.Text.Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var response = await _httpClient.PostAsync(usersEndpoint, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create user. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new ValidationException("A user with this email already exists");
                }
                
                throw new ServiceUnavailableException("Failed to create user in Keycloak");
            }

            // Get the created user's ID from the Location header
            var locationHeader = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(locationHeader))
            {
                throw new ServiceUnavailableException("Failed to retrieve created user ID");
            }

            var userId = locationHeader.Split('/').Last();
            
            _logger.LogInformation("User created with ID: {UserId}, now assigning role: {Role}", userId, roleString);
            
            await AssignRoleToUserAsync(adminToken, userId, roleString, cancellationToken);
            
            return await GetUserByIdAsync(userId, cancellationToken);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in Keycloak");
            throw new ServiceUnavailableException("Failed to create user");
        }
    }

    public async Task<KeycloakUserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
            var usersEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var response = await _httpClient.GetAsync(usersEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get user by email. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to retrieve user from Keycloak");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var users = JsonSerializer.Deserialize<List<KeycloakUserResponse>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users == null || users.Count == 0)
            {
                throw new NotFoundException($"User with email '{email}' not found");
            }

            return users[0];
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email from Keycloak");
            throw new ServiceUnavailableException("Failed to retrieve user");
        }
    }

    public async Task<KeycloakUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
            var userEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var response = await _httpClient.GetAsync(userEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new NotFoundException($"User with ID '{userId}' not found");
                }
                
                _logger.LogError("Failed to get user by ID. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to retrieve user from Keycloak");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var user = JsonSerializer.Deserialize<KeycloakUserResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (user == null)
            {
                throw new NotFoundException($"User with ID '{userId}' not found");
            }

            return user;
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID from Keycloak");
            throw new ServiceUnavailableException("Failed to retrieve user");
        }
    }

    public async Task UpdateUserAsync(string userId, string? firstName, string? lastName, bool? enabled, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
            var userEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}";

            // Get the current user to preserve existing data
            var currentUser = await GetUserByIdAsync(userId, cancellationToken);

            var updatePayload = new
            {
                firstName = firstName ?? currentUser.FirstName ?? string.Empty,
                lastName = lastName ?? currentUser.LastName ?? string.Empty,
                enabled = enabled ?? currentUser.Enabled
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(updatePayload),
                System.Text.Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var response = await _httpClient.PutAsync(userEndpoint, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to update user. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new NotFoundException($"User with ID '{userId}' not found");
                }
                
                throw new ServiceUnavailableException("Failed to update user in Keycloak");
            }

            _logger.LogInformation("Successfully updated user {UserId}", userId);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user in Keycloak");
            throw new ServiceUnavailableException("Failed to update user");
        }
    }

    public async Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
            var roleMappingsEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var response = await _httpClient.GetAsync(roleMappingsEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("User {UserId} not found when fetching roles", userId);
                    return new List<string>();
                }
                
                _logger.LogError("Failed to get user roles. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to retrieve user roles from Keycloak");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var roles = JsonSerializer.Deserialize<List<KeycloakRoleResponse>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (roles == null)
            {
                return new List<string>();
            }

            // Return only the role names
            return roles.Select(r => r.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles from Keycloak for user {UserId}", userId);
            // Return empty list instead of throwing to allow sync to continue
            return new List<string>();
        }
    }

    private async Task AssignRoleToUserAsync(string adminToken, string userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the role details from Keycloak
            var rolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/roles/{roleName}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            
            var roleResponse = await _httpClient.GetAsync(rolesEndpoint, cancellationToken);
            
            if (!roleResponse.IsSuccessStatusCode)
            {
                var errorContent = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get role {Role}. Status: {Status}, Error: {Error}",
                    roleName, roleResponse.StatusCode, errorContent);
                throw new ServiceUnavailableException($"Failed to retrieve role '{roleName}' from Keycloak");
            }
            
            var roleContent = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
            var roleData = JsonSerializer.Deserialize<JsonElement>(roleContent);
            
            var roleId = roleData.GetProperty("id").GetString();
            var roleNameFromKeycloak = roleData.GetProperty("name").GetString();
            
            var userRoleMappingEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
            
            var roleAssignment = new[]
            {
                new
                {
                    id = roleId,
                    name = roleNameFromKeycloak
                }
            };
            
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(roleAssignment),
                System.Text.Encoding.UTF8,
                "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
            
            var assignResponse = await _httpClient.PostAsync(userRoleMappingEndpoint, jsonContent, cancellationToken);
            
            if (!assignResponse.IsSuccessStatusCode)
            {
                var errorContent = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to assign role {Role} to user {UserId}. Status: {Status}, Error: {Error}",
                    roleName, userId, assignResponse.StatusCode, errorContent);
                throw new ServiceUnavailableException($"Failed to assign role '{roleName}' to user");
            }
            
            _logger.LogInformation("Successfully assigned role {Role} to user {UserId}", roleName, userId);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", roleName, userId);
            throw new ServiceUnavailableException($"Failed to assign role to user");
        }
    }

    public async Task UpdateUserRoleAsync(string userId, UserRole newRole, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
            
            var currentRoles = await GetUserRolesAsync(userId, cancellationToken);
            
            var newRoleString = newRole.ToString();
            newRoleString = char.ToLowerInvariant(newRoleString[0]) + newRoleString.Substring(1);
            
            _logger.LogInformation("Updating user {UserId} role to {Role}", userId, newRoleString);
            
            var rolesToRemove = currentRoles.Where(r => r == "orgUser" || r == "orgAdmin").ToList();
            
            if (rolesToRemove.Any())
            {
                await RemoveRolesFromUserAsync(adminToken, userId, rolesToRemove, cancellationToken);
            }
            
            await AssignRoleToUserAsync(adminToken, userId, newRoleString, cancellationToken);
            
            _logger.LogInformation("Successfully updated user {UserId} role to {Role}", userId, newRoleString);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role in Keycloak for user {UserId}", userId);
            throw new ServiceUnavailableException("Failed to update user role");
        }
    }

    private async Task RemoveRolesFromUserAsync(string adminToken, string userId, List<string> roleNames, CancellationToken cancellationToken = default)
    {
        try
        {
            var rolesToRemove = new List<object>();
            
            foreach (var roleName in roleNames)
            {
                var rolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/roles/{roleName}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
                
                var roleResponse = await _httpClient.GetAsync(rolesEndpoint, cancellationToken);
                
                if (roleResponse.IsSuccessStatusCode)
                {
                    var roleContent = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                    var roleData = JsonSerializer.Deserialize<JsonElement>(roleContent);
                    
                    var roleId = roleData.GetProperty("id").GetString();
                    var roleNameFromKeycloak = roleData.GetProperty("name").GetString();
                    
                    rolesToRemove.Add(new
                    {
                        id = roleId,
                        name = roleNameFromKeycloak
                    });
                }
            }
            
            if (rolesToRemove.Any())
            {
                var userRoleMappingEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
                
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(rolesToRemove),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
                
                var removeRequest = new HttpRequestMessage(HttpMethod.Delete, userRoleMappingEndpoint)
                {
                    Content = jsonContent
                };
                
                var removeResponse = await _httpClient.SendAsync(removeRequest, cancellationToken);
                
                if (!removeResponse.IsSuccessStatusCode)
                {
                    var errorContent = await removeResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to remove roles from user {UserId}. Status: {Status}, Error: {Error}",
                        userId, removeResponse.StatusCode, errorContent);
                    throw new ServiceUnavailableException("Failed to remove existing roles from user");
                }
                
                _logger.LogInformation("Successfully removed roles from user {UserId}", userId);
            }
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing roles from user {UserId}", userId);
            throw new ServiceUnavailableException("Failed to remove roles from user");
        }
    }

}
