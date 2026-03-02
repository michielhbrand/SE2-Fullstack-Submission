namespace Shared.Core.Keycloak.Models;

/// <summary>
/// Response containing Keycloak user information.
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
