using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.Models;

public class User
{
    [Key]
    [Required]
    [MaxLength(255)]
    public required string Id { get; set; }
    
    [Required]
    public bool Active { get; set; } = true;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
}
