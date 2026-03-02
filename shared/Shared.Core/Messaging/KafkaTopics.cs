namespace Shared.Core.Messaging;

public static class KafkaTopics
{
    public const string InvoiceCreated         = "invoice-created";
    public const string QuoteCreated           = "quote-created";
    public const string QuoteApprovalRequested = "quote-approval-requested";
    public const string InvoiceGenerated       = "invoice-generated";
    public const string InvoiceOverdue         = "invoice-overdue";
}
