namespace ManagementApi.DTOs.Auth;

/// <summary>
/// Response containing authentication tokens and user roles
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Internal response from Keycloak role endpoint
/// </summary>
internal class KeycloakRoleResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Composite { get; set; }
    public bool ClientRole { get; set; }
    public string? ContainerId { get; set; }
}
