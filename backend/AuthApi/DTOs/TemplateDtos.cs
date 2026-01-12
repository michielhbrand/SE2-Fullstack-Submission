using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

/// <summary>
/// Template data transfer object for API responses
/// </summary>
public class TemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

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

/// <summary>
/// Response DTO for template preview URL
/// </summary>
public class TemplatePreviewUrlResponse
{
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for template upload
/// </summary>
public class UploadTemplateResponse
{
    public string Name { get; set; } = string.Empty;
}
