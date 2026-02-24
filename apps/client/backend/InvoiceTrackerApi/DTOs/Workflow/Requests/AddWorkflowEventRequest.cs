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

    /// <summary>
    /// Number of days until invoice is due (only used for ConvertedToInvoice event). Defaults to 30.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Pay by days must be between 1 and 365")]
    public int PayByDays { get; set; } = 30;
}
