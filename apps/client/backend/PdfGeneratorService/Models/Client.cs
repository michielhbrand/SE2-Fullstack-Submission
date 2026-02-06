using System.ComponentModel.DataAnnotations;

namespace PdfGeneratorService.Models;

public class Client
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Cellphone { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Company { get; set; }

    [Required]
    public DateTime DateCreated { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    [MaxLength(255)]
    public string? ModifiedBy { get; set; }

    // Keycloak user ID reference
    [MaxLength(255)]
    public string? KeycloakUserId { get; set; }
}
