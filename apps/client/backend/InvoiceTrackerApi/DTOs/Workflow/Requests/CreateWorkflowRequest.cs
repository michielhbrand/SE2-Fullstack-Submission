using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Workflow.Requests;

/// <summary>
/// Request DTO for creating a new workflow
/// </summary>
public class CreateWorkflowRequest
{
    /// <summary>
    /// Workflow type: "QuoteFirst" or "InvoiceFirst"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The client this workflow is for
    /// </summary>
    [Required]
    public int ClientId { get; set; }

    /// <summary>
    /// Optional linked quote (required for QuoteFirst workflows)
    /// </summary>
    public int? QuoteId { get; set; }

    /// <summary>
    /// Optional linked invoice (required for InvoiceFirst workflows)
    /// </summary>
    public int? InvoiceId { get; set; }
}
