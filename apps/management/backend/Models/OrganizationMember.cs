namespace ManagementApi.Models;

public class OrganizationMember
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public required string UserId { get; set; }  // Keycloak user ID (UUID string)
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
