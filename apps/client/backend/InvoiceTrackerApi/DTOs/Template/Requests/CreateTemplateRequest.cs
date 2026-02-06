using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Template.Requests;

/// <summary>
/// Request DTO for creating a new template
/// </summary>
public class CreateTemplateRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Version { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
}
