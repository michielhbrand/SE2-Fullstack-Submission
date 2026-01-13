using System.Text.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace AuthApi.Services.Auth;

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

    public async Task<TokenResponse?> LoginAsync(string username, string password, bool isAdminLogin = false)
    {
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
            
            _logger.LogInformation("Attempting {LoginType} login for user: {Username} at endpoint: {Endpoint}",
                isAdminLogin ? "admin" : "normal", username, tokenEndpoint);
            
            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for user: {Username}. Status: {Status}, Error: {Error}",
                    username, response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keycloakResponse == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                return null;
            }

            // Extract roles from token
            var roles = ExtractRolesFromToken(keycloakResponse.Access_Token);
            
            // If admin login, verify user has admin role
            if (isAdminLogin && !roles.Contains("admin"))
            {
                _logger.LogWarning("User {Username} attempted admin login without admin role", username);
                return null;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", username);
            return null;
        }
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        try
        {
            var logoutEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/logout";
            
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "refresh_token", refreshToken }
            };

            var content = new FormUrlEncodedContent(requestData);
            
            _logger.LogInformation("Attempting logout at endpoint: {Endpoint}", logoutEndpoint);
            
            var response = await _httpClient.PostAsync(logoutEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Logout failed. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Logout successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
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
                var roles = await GetUserRolesAsync(adminToken, user.Id);
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

    public async Task<UpdateRoleResult> UpdateUserRoleAsync(string adminToken, string userId, bool isAdmin)
    {
        try
        {
            // Prevent self-demotion: Extract current user ID from token
            var currentUserId = ExtractUserIdFromToken(adminToken);
            if (!string.IsNullOrEmpty(currentUserId) && currentUserId == userId && !isAdmin)
            {
                _logger.LogWarning("User {UserId} attempted to demote themselves, which is not allowed", userId);
                return UpdateRoleResult.Failed("You cannot demote yourself");
            }

            // Get available realm roles to find the admin role
            var availableRolesEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/roles";
            _logger.LogInformation("Attempting to get available roles from: {Endpoint}", availableRolesEndpoint);
            
            var rolesRequest = new HttpRequestMessage(HttpMethod.Get, availableRolesEndpoint);
            rolesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var rolesResponse = await _httpClient.SendAsync(rolesRequest);
            if (!rolesResponse.IsSuccessStatusCode)
            {
                var errorContent = await rolesResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get available roles. Status: {Status}, Error: {Error}", rolesResponse.StatusCode, errorContent);
                return UpdateRoleResult.Failed($"Failed to retrieve available roles: {rolesResponse.StatusCode}");
            }
            
            var rolesContent = await rolesResponse.Content.ReadAsStringAsync();
            var allRoles = JsonSerializer.Deserialize<List<KeycloakRole>>(rolesContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var adminRole = allRoles?.FirstOrDefault(r => r.Name.Equals("admin", StringComparison.OrdinalIgnoreCase));
            
            if (adminRole == null)
            {
                _logger.LogWarning("Admin role not found in available roles");
                return UpdateRoleResult.Failed("Admin role not found in realm");
            }
            
            _logger.LogInformation("Found admin role with ID: {RoleId}", adminRole.Id);
            
            // Add or remove admin role
            var userRoleEndpoint = $"{_keycloakUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm";
            var roleArray = new[] { new { id = adminRole.Id, name = adminRole.Name } };
            var jsonContent = new StringContent(JsonSerializer.Serialize(roleArray), Encoding.UTF8, "application/json");
            
            HttpResponseMessage roleResponse;
            if (isAdmin)
            {
                // Add admin role
                var addRequest = new HttpRequestMessage(HttpMethod.Post, userRoleEndpoint);
                addRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                addRequest.Content = jsonContent;
                roleResponse = await _httpClient.SendAsync(addRequest);
            }
            else
            {
                // Remove admin role
                var removeRequest = new HttpRequestMessage(HttpMethod.Delete, userRoleEndpoint);
                removeRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
                removeRequest.Content = jsonContent;
                roleResponse = await _httpClient.SendAsync(removeRequest);
            }
            
            if (!roleResponse.IsSuccessStatusCode)
            {
                var errorContent = await roleResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update user role. Status: {Status}, Error: {Error}",
                    roleResponse.StatusCode, errorContent);
                return UpdateRoleResult.Failed("Failed to update user role in Keycloak");
            }
            
            _logger.LogInformation("Successfully {Action} admin role for user {UserId}",
                isAdmin ? "added" : "removed", userId);
            return UpdateRoleResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role");
            return UpdateRoleResult.Failed("An unexpected error occurred while updating user role");
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
