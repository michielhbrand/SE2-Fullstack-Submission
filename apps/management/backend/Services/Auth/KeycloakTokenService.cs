using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using ManagementApi.DTOs.Auth;
using Microsoft.Extensions.Options;
using Shared.Core.Keycloak.Models;
using Shared.Core.Exceptions.Application;
using Shared.Core.Exceptions;

namespace ManagementApi.Services.Auth;

public class KeycloakTokenService : IKeycloakTokenService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakTokenService> _logger;
    private readonly string _keycloakUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _adminUsername;
    private readonly string _adminPassword;

    public KeycloakTokenService(
        HttpClient httpClient,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakTokenService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var opts = options.Value;
        _clientId = opts.ClientId;
        _adminUsername = opts.AdminUsername;
        _adminPassword = opts.AdminPassword;

        var authorityUri = new Uri(opts.Authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _keycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        _realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";

        _logger.LogInformation("KeycloakTokenService initialized - URL: {Url}, Realm: {Realm}, ClientId: {ClientId}",
            _keycloakUrl, _realm, _clientId);
    }

    public async Task<TokenResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ValidationException("Username and password are required");

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

            _logger.LogInformation("Attempting systemAdmin login for user: {Username}", username);

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed for user: {Username}. Status: {Status}", username, response.StatusCode);
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
        catch (AppException) { throw; }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for user: {Username}", username);
            throw new ServiceUnavailableException("Authentication service is unavailable");
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
            throw new ValidationException("Refresh token is required");

        try
        {
            var tokenEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/token";
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed. Status: {Status}", response.StatusCode);
                throw new UnauthorizedException("Refresh token is invalid or expired");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var keycloakResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (keycloakResponse == null)
                throw new ServiceUnavailableException("Authentication service returned invalid response");

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
        catch (AppException) { throw; }
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
            throw new ValidationException("Refresh token is required");

        try
        {
            var logoutEndpoint = $"{_keycloakUrl}/realms/{_realm}/protocol/openid-connect/logout";
            var requestData = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "refresh_token", refreshToken }
            };

            var response = await _httpClient.PostAsync(logoutEndpoint, new FormUrlEncodedContent(requestData), cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.BadRequest)
                _logger.LogWarning("Logout failed. Status: {Status}", response.StatusCode);

            _logger.LogInformation("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Don't throw - logout should be best effort
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

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get admin access token. Status: {Status}", response.StatusCode);
                throw new ServiceUnavailableException("Failed to authenticate with Keycloak admin API");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Access_Token))
                throw new ServiceUnavailableException("Invalid admin token response");

            return tokenResponse.Access_Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin access token");
            throw;
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
}
