using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.Models;

public class UserDirectory
{
    [Key]
    [Required]
    [MaxLength(255)]
    public required string Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string Email { get; set; }
    
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(500)]
    public string? Roles { get; set; }
    
    [Required]
    public bool KeycloakEnabled { get; set; } = true;
    
    [Required]
    public bool Active { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public DateTime LastSyncedAt { get; set; }
}
