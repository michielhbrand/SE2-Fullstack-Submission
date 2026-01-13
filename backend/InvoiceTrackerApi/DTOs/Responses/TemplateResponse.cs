namespace InvoiceTrackerApi.DTOs.Responses;

/// <summary>
/// Template data transfer object for API responses
/// </summary>
public class TemplateResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for template upload
/// </summary>
public class UploadTemplateResponse
{
    public string Name { get; set; } = string.Empty;
}
