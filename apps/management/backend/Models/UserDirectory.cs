namespace ManagementApi.Models;

public class UserDirectory
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Roles { get; set; }
    public bool KeycloakEnabled { get; set; } = true;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
