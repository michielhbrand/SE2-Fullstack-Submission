using Shared.Database.Models;

namespace InvoiceTrackerApi.Services.Workflow;

/// <summary>
/// Static state machine defining valid workflow transitions and event-to-status mappings.
/// </summary>
public static class WorkflowStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        [WorkflowStatus.Draft] = new HashSet<string>
        {
            WorkflowStatus.PendingApproval,
            WorkflowStatus.InvoiceCreated,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.PendingApproval] = new HashSet<string>
        {
            WorkflowStatus.PendingApproval,
            WorkflowStatus.Approved,
            WorkflowStatus.Rejected,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.Approved] = new HashSet<string>
        {
            WorkflowStatus.InvoiceCreated,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.Rejected] = new HashSet<string>
        {
            WorkflowStatus.PendingApproval,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.InvoiceCreated] = new HashSet<string>
        {
            WorkflowStatus.SentForPayment,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.SentForPayment] = new HashSet<string>
        {
            WorkflowStatus.SentForPayment,
            WorkflowStatus.Paid,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.Paid]       = new HashSet<string>(),
        [WorkflowStatus.Cancelled]  = new HashSet<string>(),
        [WorkflowStatus.Terminated] = new HashSet<string>()
    };

    private static readonly Dictionary<string, string> EventToStatusMap = new()
    {
        [WorkflowEventType.QuoteCreated]        = WorkflowStatus.Draft,
        [WorkflowEventType.SentForApproval]     = WorkflowStatus.PendingApproval,
        [WorkflowEventType.Approved]            = WorkflowStatus.Approved,
        [WorkflowEventType.Rejected]            = WorkflowStatus.Rejected,
        [WorkflowEventType.QuoteModified]       = WorkflowStatus.Rejected,
        [WorkflowEventType.ResentForApproval]   = WorkflowStatus.PendingApproval,
        [WorkflowEventType.ConvertedToInvoice]  = WorkflowStatus.InvoiceCreated,
        [WorkflowEventType.InvoiceCreated]      = WorkflowStatus.InvoiceCreated,
        [WorkflowEventType.SentForPayment]      = WorkflowStatus.SentForPayment,
        [WorkflowEventType.ResentForPayment]    = WorkflowStatus.SentForPayment,
        [WorkflowEventType.OverdueReminderSent] = WorkflowStatus.SentForPayment,
        [WorkflowEventType.MarkedAsPaid]        = WorkflowStatus.Paid,
        [WorkflowEventType.Cancelled]           = WorkflowStatus.Cancelled,
        [WorkflowEventType.Terminated]          = WorkflowStatus.Terminated
    };

    public static bool IsValidTransition(string from, string to)
    {
        return ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static string? GetTargetStatus(string eventType)
    {
        return EventToStatusMap.TryGetValue(eventType, out var status) ? status : null;
    }

    public static bool IsKnownEventType(string eventType) => EventToStatusMap.ContainsKey(eventType);

    public static bool IsTerminal(string status)
    {
        return status is WorkflowStatus.Paid or WorkflowStatus.Cancelled or WorkflowStatus.Terminated;
    }
}
