using System.Text.Json;

namespace AuthApi.Services;

public class KeycloakAuthService : IKeycloakAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAuthService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _clientId;

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
        
        _logger.LogInformation("KeycloakAuthService initialized with URL: {Url}, Realm: {Realm}, ClientId: {ClientId}", 
            _keycloakUrl, _realm, _clientId);
    }

    public async Task<TokenResponse?> LoginAsync(string username, string password)
    {
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
            
            _logger.LogInformation("Attempting login for user: {Username} at endpoint: {Endpoint}", username, tokenEndpoint);
            
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

            _logger.LogInformation("Login successful for user: {Username}", username);

            return new TokenResponse
            {
                AccessToken = keycloakResponse.Access_Token,
                RefreshToken = keycloakResponse.Refresh_Token,
                ExpiresIn = keycloakResponse.Expires_In,
                TokenType = keycloakResponse.Token_Type
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

    // Internal class to match Keycloak's JSON response format
    private class KeycloakTokenResponse
    {
        public string Access_Token { get; set; } = string.Empty;
        public string Refresh_Token { get; set; } = string.Empty;
        public int Expires_In { get; set; }
        public string Token_Type { get; set; } = string.Empty;
    }
}
