namespace Shared.Core.Keycloak.Models;

/// <summary>
/// Internal response from Keycloak token endpoint.
/// </summary>
public class KeycloakTokenResponse
{
    public string Access_Token { get; set; } = string.Empty;
    public string Refresh_Token { get; set; } = string.Empty;
    public int Expires_In { get; set; }
    public string Token_Type { get; set; } = string.Empty;
}
