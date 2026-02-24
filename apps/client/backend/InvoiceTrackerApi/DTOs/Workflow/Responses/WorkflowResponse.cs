namespace InvoiceTrackerApi.DTOs.Workflow.Responses;

/// <summary>
/// Full workflow response with nested events for detail view
/// </summary>
public class WorkflowResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public int? QuoteId { get; set; }
    public int? InvoiceId { get; set; }
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }
    public List<WorkflowEventResponse> Events { get; set; } = new();
}
