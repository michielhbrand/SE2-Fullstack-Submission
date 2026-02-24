namespace InvoiceTrackerApi.DTOs.Workflow.Responses;

/// <summary>
/// Workflow event response for timeline items
/// </summary>
public class WorkflowEventResponse
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime OccurredAt { get; set; }
}
