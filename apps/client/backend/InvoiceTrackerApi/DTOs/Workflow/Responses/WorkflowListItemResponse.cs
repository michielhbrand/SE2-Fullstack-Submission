namespace InvoiceTrackerApi.DTOs.Workflow.Responses;

/// <summary>
/// Lightweight workflow response for list views
/// </summary>
public class WorkflowListItemResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? QuoteId { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
