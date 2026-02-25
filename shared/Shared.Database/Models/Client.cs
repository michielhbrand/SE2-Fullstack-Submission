using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class Client
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Cellphone { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required]
    public bool IsCompany { get; set; }

    [MaxLength(50)]
    public string? VatNumber { get; set; }

    [Required]
    public DateTime DateCreated { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    [MaxLength(255)]
    public string? ModifiedBy { get; set; }

    // Keycloak user ID reference
    [MaxLength(255)]
    public string? KeycloakUserId { get; set; }

    // Organization scoping
    public int OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;
}
