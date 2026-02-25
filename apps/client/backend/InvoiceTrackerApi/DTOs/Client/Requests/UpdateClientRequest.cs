using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Client.Requests;

/// <summary>
/// Request DTO for updating an existing client
/// </summary>
public class UpdateClientRequest
{
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

    [MaxLength(255)]
    public string? KeycloakUserId { get; set; }
}
