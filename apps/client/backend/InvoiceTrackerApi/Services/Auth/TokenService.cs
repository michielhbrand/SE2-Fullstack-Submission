using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Service for JWT token operations including parsing and extracting claims.
/// </summary>
public class TokenService
{
    private readonly ILogger<TokenService> _logger;

    public TokenService(ILogger<TokenService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts roles from a JWT token's realm_access claim.
    /// </summary>
    public List<string> ExtractRolesFromToken(string token)
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

    /// <summary>
    /// Extracts the user ID (sub claim) from a JWT token.
    /// </summary>
    public string? ExtractUserIdFromToken(string token)
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
}
