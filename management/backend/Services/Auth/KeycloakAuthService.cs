using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using ManagementApi.Exceptions;
using ManagementApi.Exceptions.Application;

namespace ManagementApi.Services.Auth;

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

        var authority = _configuration["Keycloak:Authority"] 
            ?? throw new InvalidOperationException("Keycloak:Authority not configured");
        
        var authorityUri = new Uri(authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        _keycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        _realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";
        _clientId = _configuration["Keycloak:ClientId"] ?? "management-portal";
        
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

    private class KeycloakTokenResponse
    {
        public string Access_Token { get; set; } = string.Empty;
        public string Refresh_Token { get; set; } = string.Empty;
        public int Expires_In { get; set; }
        public string Token_Type { get; set; } = string.Empty;
    }
}
