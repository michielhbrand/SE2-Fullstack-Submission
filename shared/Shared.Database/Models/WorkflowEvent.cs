using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class WorkflowEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkflowId { get; set; }

    [ForeignKey(nameof(WorkflowId))]
    public Workflow? Workflow { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? PerformedBy { get; set; }

    [Required]
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Workflow event type constants
/// </summary>
public static class WorkflowEventType
{
    public const string QuoteCreated = "QuoteCreated";
    public const string SentForApproval = "SentForApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string QuoteModified = "QuoteModified";
    public const string ResentForApproval = "ResentForApproval";
    public const string ConvertedToInvoice = "ConvertedToInvoice";
    public const string InvoiceCreated = "InvoiceCreated";
    public const string SentForPayment = "SentForPayment";
    public const string ResentForPayment = "ResentForPayment";
    public const string MarkedAsPaid = "MarkedAsPaid";
    public const string Cancelled = "Cancelled";
    public const string Terminated = "Terminated";
    public const string OverdueReminderSent = "OverdueReminderSent";
}
