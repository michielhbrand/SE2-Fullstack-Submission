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
/// Response containing Keycloak user information
/// </summary>
public class KeycloakUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public long CreatedTimestamp { get; set; }
}

/// <summary>
/// Internal response from Keycloak token endpoint
/// </summary>
internal class KeycloakTokenResponse
{
    public string Access_Token { get; set; } = string.Empty;
    public string Refresh_Token { get; set; } = string.Empty;
    public int Expires_In { get; set; }
    public string Token_Type { get; set; } = string.Empty;
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
