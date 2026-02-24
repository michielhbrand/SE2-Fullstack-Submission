using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class Workflow
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = WorkflowStatus.Draft;

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    // Organization scope
    [Required]
    public int OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }

    // Linked documents (nullable — quote may not exist for invoice-first)
    public int? QuoteId { get; set; }

    [ForeignKey(nameof(QuoteId))]
    public Quote? Quote { get; set; }

    public int? InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice? Invoice { get; set; }

    // Linked client
    [Required]
    public int ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    // Soft-delete support — workflows are never hard-deleted
    public bool IsActive { get; set; } = true;

    // Timeline events
    public ICollection<WorkflowEvent> Events { get; set; } = new List<WorkflowEvent>();
}

/// <summary>
/// Workflow type constants
/// </summary>
public static class WorkflowType
{
    public const string QuoteFirst = "QuoteFirst";
    public const string InvoiceFirst = "InvoiceFirst";
}

/// <summary>
/// Workflow status constants
/// </summary>
public static class WorkflowStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string InvoiceCreated = "InvoiceCreated";
    public const string SentForPayment = "SentForPayment";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
    public const string Terminated = "Terminated";
}
