namespace InvoiceTrackerApi.Services.Auth.Models;

/// <summary>
/// Models for Keycloak API responses.
/// These match Keycloak's JSON response formats.
/// </summary>

public class KeycloakTokenResponse
{
    public string Access_Token { get; set; } = string.Empty;
    public string Refresh_Token { get; set; } = string.Empty;
    public int Expires_In { get; set; }
    public string Token_Type { get; set; } = string.Empty;
}

public class KeycloakUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
}

public class KeycloakRole
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
