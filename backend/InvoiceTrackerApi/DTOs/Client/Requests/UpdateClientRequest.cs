using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Client.Requests;

/// <summary>
/// Request DTO for updating an existing client
/// </summary>
public class UpdateClientRequest
{
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

    [MaxLength(255)]
    public string? KeycloakUserId { get; set; }
}
