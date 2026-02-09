using System.Text;
using System.Text.Json;
using InvoiceTrackerApi.Services.Auth.Models;

namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Client for Keycloak Admin API operations.
/// Handles authentication and low-level API calls to Keycloak.
/// </summary>
public class KeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakConfiguration _config;
    private readonly ILogger<KeycloakAdminClient> _logger;

    public KeycloakAdminClient(
        HttpClient httpClient,
        KeycloakConfiguration config,
        ILogger<KeycloakAdminClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Obtains an admin access token for Keycloak Admin API operations.
    /// </summary>
    public async Task<string> GetAdminAccessTokenAsync()
    {
        try
        {
            var requestData = new Dictionary<string, string>
            {
                { "client_id", "admin-cli" },
                { "grant_type", "password" },
                { "username", _config.AdminUsername },
                { "password", _config.AdminPassword }
            };

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(_config.GetAdminTokenEndpoint(), content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get admin access token. Status: {Status}", response.StatusCode);
                throw new Exceptions.InfrastructureException("Failed to authenticate with Keycloak admin API");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Access_Token))
            {
                throw new Exceptions.InfrastructureException("Invalid admin token response");
            }

            return tokenResponse.Access_Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin access token");
            throw new Exceptions.InfrastructureException("Failed to obtain admin access token", ex);
        }
    }

    /// <summary>
    /// Gets all users from Keycloak.
    /// </summary>
    public async Task<List<KeycloakUser>> GetAllUsersAsync(string adminToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _config.GetUsersEndpoint());
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get users. Status: {Status}", response.StatusCode);
            return new List<KeycloakUser>();
        }
        
        var content = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return users ?? new List<KeycloakUser>();
    }

    /// <summary>
    /// Gets all available realm roles.
    /// </summary>
    public async Task<List<KeycloakRole>> GetAllRolesAsync(string adminToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _config.GetRolesEndpoint());
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get available roles. Status: {Status}", response.StatusCode);
            throw new Exceptions.BusinessRuleException("Failed to retrieve available roles from authentication service");
        }
        
        var content = await response.Content.ReadAsStringAsync();
        var roles = JsonSerializer.Deserialize<List<KeycloakRole>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return roles ?? new List<KeycloakRole>();
    }

    /// <summary>
    /// Gets roles assigned to a specific user.
    /// </summary>
    public async Task<List<string>> GetUserRolesAsync(string adminToken, string userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _config.GetUserRoleMappingEndpoint(userId));
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

    /// <summary>
    /// Creates a new user in Keycloak.
    /// Returns the Location header containing the new user's ID.
    /// </summary>
    public async Task<string> CreateUserAsync(string adminToken, string username, string email, 
        string firstName, string lastName, string password)
    {
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
        
        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetUsersEndpoint());
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = jsonContent;
        
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
        
        return locationHeader.Split('/').Last();
    }

    /// <summary>
    /// Removes roles from a user.
    /// </summary>
    public async Task RemoveUserRolesAsync(string adminToken, string userId, List<KeycloakRole> rolesToRemove)
    {
        if (!rolesToRemove.Any()) return;

        var rolesToRemoveObjects = rolesToRemove
            .Select(r => new { id = r.Id, name = r.Name })
            .ToArray();
        
        var removeContent = new StringContent(
            JsonSerializer.Serialize(rolesToRemoveObjects), 
            Encoding.UTF8, 
            "application/json");
        
        var removeRequest = new HttpRequestMessage(HttpMethod.Delete, _config.GetUserRoleMappingEndpoint(userId));
        removeRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        removeRequest.Content = removeContent;
        
        await _httpClient.SendAsync(removeRequest);
    }

    /// <summary>
    /// Adds a role to a user.
    /// </summary>
    public async Task AddUserRoleAsync(string adminToken, string userId, KeycloakRole role)
    {
        var roleArray = new[] { new { id = role.Id, name = role.Name } };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(roleArray), 
            Encoding.UTF8, 
            "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetUserRoleMappingEndpoint(userId));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = jsonContent;
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to add user role. Status: {Status}", response.StatusCode);
            throw new Exceptions.BusinessRuleException("Failed to update user role in Keycloak");
        }
    }
}
