namespace ManagementApi.Models;

public class User
{
    public required string Id { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;    
    public DateTime? UpdatedAt { get; set; }
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
}
