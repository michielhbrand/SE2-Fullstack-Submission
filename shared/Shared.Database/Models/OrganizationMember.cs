using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class OrganizationMember
{
    [Required]
    public int OrganizationId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public required string UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string Role { get; set; }
    
    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }
}
