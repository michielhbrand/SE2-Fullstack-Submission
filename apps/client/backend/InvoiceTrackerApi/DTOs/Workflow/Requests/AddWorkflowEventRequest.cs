using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Workflow.Requests;

/// <summary>
/// Request DTO for adding an event to a workflow
/// </summary>
public class AddWorkflowEventRequest
{
    /// <summary>
    /// The event type (e.g., SentForApproval, Approved, Rejected, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable description/note
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
}
