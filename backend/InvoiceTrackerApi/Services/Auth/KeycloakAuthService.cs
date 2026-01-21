using System.Text.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace InvoiceTrackerApi.Services.Auth;

public class KeycloakAuthService : IKeycloakAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAuthService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _adminClientId;

    public KeycloakAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Extract Keycloak configuration
        var authority = _configuration["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority not configured");
        
        // Parse authority to extract base URL and realm
        // Authority format: http://localhost:9090/realms/microservices
        var authorityUri = new Uri(authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        _keycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        _realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";
        _clientId = _configuration["Keycloak:ClientId"] ?? "frontend-app";
        _adminClientId = "admin-app";
        
        _logger.LogInformation("KeycloakAuthService initialized with URL: {Url}, Realm: {Realm}, ClientId: {ClientId}",
            _keycloakUrl, _realm, _clientId);
    }

    public async Task<TokenResponse> LoginAsync(string username, string password, bool isAdminLogin = false)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new Exceptions.ValidationException("Username and password are required");
        }

        try
        {
            var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";
            var clientId = isAdminLogin ? _adminClientId : _clientId;
            
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
            
            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for user: {Username}. Status: {Status}", username, response.StatusCode);
                throw new Exceptions.UnauthorizedException("Invalid username or password");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keycloakResponse == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                throw new Exceptions.InfrastructureException("Authentication service returned invalid response");
            }

            // Extract roles from token
            var roles = ExtractRolesFromToken(keycloakResponse.Access_Token);
            
            // If admin login, verify user has orgAdmin or systemAdmin role
            if (isAdminLogin && !roles.Contains(UserRole.OrgAdmin.ToRoleString()) && !roles.Contains(UserRole.SystemAdmin.ToRoleString()))
            {
                _logger.LogWarning("User {Username} attempted admin login without admin role", username);
                throw new Exceptions.ForbiddenException("User does not have admin privileges");
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
        catch (Exceptions.AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for user: {Username}", username);
            throw new Exceptions.InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user: {Username}", username);
            throw new Exceptions.InfrastructureException("An unexpected error occurred during authentication", ex);
        }
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new Exceptions.ValidationException("Refresh token is required");
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
            
            _logger.LogInformation("Attempting to refresh token");
            
            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Token refresh failed. Status: {Status}", response.StatusCode);
                throw new Exceptions.UnauthorizedException("Refresh token is invalid or expired");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keycloakResponse == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                throw new Exceptions.InfrastructureException("Authentication service returned invalid response");
            }

            // Extract roles from token
            var roles = ExtractRolesFromToken(keycloakResponse.Access_Token);
            
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
        catch (Exceptions.AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during token refresh");
            throw new Exceptions.InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            throw new Exceptions.InfrastructureException("An unexpected error occurred during token refresh", ex);
        }
    }

    public async Task LogoutAsync(string refreshToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new Exceptions.ValidationException("Refresh token is required");
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
            
            _logger.LogInformation("Attempting logout");
            
            var response = await _httpClient.PostAsync(logoutEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Logout failed. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                
                // If the token is already invalid/expired, treat it as a successful logout
                // since the session is already terminated on Keycloak's side
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogInformation("Refresh token already invalid/expired - treating as successful logout");
                    return;
                }
                
                throw new Exceptions.BusinessRuleException("Unable to logout. The refresh token may be invalid or expired.");
            }

            _logger.LogInformation("Logout successful");
        }
        catch (Exceptions.AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during logout");
            throw new Exceptions.InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            throw new Exceptions.InfrastructureException("An unexpected error occurred during logout", ex);
        }
    }

    public async Task<List<UserInfo>> GetAllUsersAsync(string adminToken)
    {
        try
        {
            var usersEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users";
            
            var request = new HttpRequestMessage(HttpMethod.Get, usersEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get users. Status: {Status}", response.StatusCode);
                return new List<UserInfo>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var keycloakUsers = JsonSerializer.Deserialize<List<KeycloakUser>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (keycloakUsers == null) return new List<UserInfo>();
            
            var users = new List<UserInfo>();
            foreach (var user in keycloakUsers)
            {
                // Filter out service accounts and users without proper usernames
                if (string.IsNullOrWhiteSpace(user.Username) ||
                    user.Username.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var roles = await GetUserRolesAsync(adminToken, user.Id);
                
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
                throw new Exceptions.ForbiddenException("Cannot assign System Admin role through this endpoint. System Admin role can only be managed directly in Keycloak.");
            }

            // Validate role - only OrgUser and OrgAdmin are assignable
            if (role != UserRole.OrgUser && role != UserRole.OrgAdmin)
            {
                throw new Exceptions.ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
            }

            // Convert enum to string for Keycloak API
            var roleString = role.ToRoleString();

            // Prevent self-demotion: Extract current user ID from token
            var currentUserId = ExtractUserIdFromToken(adminToken);
            var currentUserRoles = await GetUserRolesAsync(adminToken, currentUserId ?? string.Empty);
            var isCurrentUserAdmin = currentUserRoles.Contains(UserRole.OrgAdmin.ToRoleString()) || currentUserRoles.Contains(UserRole.SystemAdmin.ToRoleString());
            
            if (!string.IsNullOrEmpty(currentUserId) && currentUserId == userId && role == UserRole.OrgUser && isCurrentUserAdmin)
            {
                _logger.LogWarning("User {UserId} attempted to demote themselves, which is not allowed", userId);
                throw new Exceptions.ForbiddenException("You cannot demote yourself");
            }

            // Get available realm roles
            var availableRolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/roles";
            
            var rolesRequest = new HttpRequestMessage(HttpMethod.Get, availableRolesEndpoint);
            rolesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var rolesResponse = await _httpClient.SendAsync(rolesRequest);
            if (!rolesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get available roles. Status: {Status}", rolesResponse.StatusCode);
                throw new Exceptions.BusinessRuleException($"Failed to retrieve available roles from authentication service");
            }
            
            var rolesContent = await rolesResponse.Content.ReadAsStringAsync();
            var allRoles = JsonSerializer.Deserialize<List<KeycloakRole>>(rolesContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            // Find the target role
            var targetRole = allRoles?.FirstOrDefault(r => r.Name.Equals(roleString, StringComparison.OrdinalIgnoreCase));
            
            if (targetRole == null)
            {
                _logger.LogWarning("Role {Role} not found in available roles", roleString);
                throw new Exceptions.BusinessRuleException($"Role '{roleString}' not found in realm");
            }
            
            _logger.LogInformation("Found role {Role} with ID: {RoleId}", roleString, targetRole.Id);
            
            // Get current user roles to remove
            var currentRoles = await GetUserRolesAsync(adminToken, userId);
            var validRoles = UserRoleExtensions.GetAssignableRoleStrings();
            var rolesToRemove = currentRoles.Where(r => validRoles.Contains(r)).ToList();
            
            var userRoleEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
            
            // Remove existing roles
            if (rolesToRemove.Any())
            {
                var rolesToRemoveObjects = allRoles?
                    .Where(r => rolesToRemove.Contains(r.Name))
                    .Select(r => new { id = r.Id, name = r.Name })
                    .ToArray();
                
                if (rolesToRemoveObjects != null && rolesToRemoveObjects.Any())
                {
                    var removeContent = new StringContent(JsonSerializer.Serialize(rolesToRemoveObjects), Encoding.UTF8, "application/json");
                    var removeRequest = new HttpRequestMessage(HttpMethod.Delete, userRoleEndpoint);
                    removeRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                    removeRequest.Content = removeContent;
                    await _httpClient.SendAsync(removeRequest);
                }
            }
            
            // Add new role
            var roleArray = new[] { new { id = targetRole.Id, name = targetRole.Name } };
            var jsonContent = new StringContent(JsonSerializer.Serialize(roleArray), Encoding.UTF8, "application/json");
            
            var addRequest = new HttpRequestMessage(HttpMethod.Post, userRoleEndpoint);
            addRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            addRequest.Content = jsonContent;
            var roleResponse = await _httpClient.SendAsync(addRequest);
            
            if (!roleResponse.IsSuccessStatusCode)
            {
                var errorContent = await roleResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update user role. Status: {Status}", roleResponse.StatusCode);
                throw new Exceptions.BusinessRuleException("Failed to update user role in Keycloak");
            }
            
            _logger.LogInformation("Successfully updated user {UserId} to role {Role}", userId, roleString);
        }
        catch (Exceptions.AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error updating user role");
            throw new Exceptions.InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user role");
            throw new Exceptions.InfrastructureException("An unexpected error occurred while updating user role", ex);
        }
    }

    public async Task<string> CreateUserAsync(string adminToken, string username, string email, string firstName, string lastName, string password, UserRole role)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new Exceptions.ValidationException("Username is required");
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exceptions.ValidationException("Email is required");
        }
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new Exceptions.ValidationException("First name is required");
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new Exceptions.ValidationException("Last name is required");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new Exceptions.ValidationException("Password is required");
        }

        // CRITICAL: Prevent creation of systemAdmin users
        if (role == UserRole.SystemAdmin)
        {
            _logger.LogWarning("Attempt to create systemAdmin user '{Username}' was blocked", username);
            throw new Exceptions.ForbiddenException("Cannot create System Admin users through this endpoint. System Admin role can only be managed directly in Keycloak.");
        }

        // Validate role - only OrgUser and OrgAdmin are allowed
        if (role != UserRole.OrgUser && role != UserRole.OrgAdmin)
        {
            throw new Exceptions.ValidationException($"Invalid role. Must be one of: {string.Join(", ", UserRoleExtensions.GetAssignableRoleStrings())}");
        }

        try
        {
            var createUserEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users";
            
            // Prepare user creation payload
            var userPayload = new
            {
                username = username,
                email = email,
                firstName = firstName,
                lastName = lastName,
                enabled = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
            };
            
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(userPayload),
                Encoding.UTF8,
                "application/json"
            );
            
            var request = new HttpRequestMessage(HttpMethod.Post, createUserEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            request.Content = jsonContent;
            
            _logger.LogInformation("Creating new user: {Username} with role: {Role}", username, role.ToRoleString());
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create user {Username}. Status: {Status}, Error: {Error}",
                    username, response.StatusCode, errorContent);
                
                // Check for conflict (user already exists)
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new Exceptions.ConflictException("A user with this username or email already exists");
                }
                
                throw new Exceptions.BusinessRuleException("Failed to create user in Keycloak");
            }
            
            // Extract user ID from Location header
            var locationHeader = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(locationHeader))
            {
                _logger.LogError("Failed to get user ID from Location header after creating user {Username}", username);
                throw new Exceptions.BusinessRuleException("Failed to retrieve created user ID");
            }
            
            var userId = locationHeader.Split('/').Last();
            _logger.LogInformation("User {Username} created successfully with ID: {UserId}", username, userId);
            
            // Assign role to the newly created user
            await UpdateUserRoleAsync(adminToken, userId, role);
            
            _logger.LogInformation("Successfully created user {Username} with role {Role}", username, role.ToRoleString());
            
            return userId;
        }
        catch (Exceptions.AppException)
        {
            // Re-throw application exceptions
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating user {Username}", username);
            throw new Exceptions.InfrastructureException("Authentication service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating user {Username}", username);
            throw new Exceptions.InfrastructureException("An unexpected error occurred while creating user", ex);
        }
    }

    private async Task<List<string>> GetUserRolesAsync(string adminToken, string userId)
    {
        try
        {
            var rolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
            
            var request = new HttpRequestMessage(HttpMethod.Get, rolesEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var roles = JsonSerializer.Deserialize<List<KeycloakRole>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return roles?.Select(r => r.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    private List<string> ExtractRolesFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Keycloak stores realm roles in realm_access.roles
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

    private string? ExtractUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Keycloak stores user ID in 'sub' claim
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
            return subClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return null;
        }
    }

    // Internal classes to match Keycloak's JSON response formats
    private class KeycloakTokenResponse
    {
        public string Access_Token { get; set; } = string.Empty;
        public string Refresh_Token { get; set; } = string.Empty;
        public int Expires_In { get; set; }
        public string Token_Type { get; set; } = string.Empty;
    }

    private class KeycloakUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; }
    }

    private class KeycloakRole
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
