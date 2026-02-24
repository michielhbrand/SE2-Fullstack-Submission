namespace InvoiceTrackerApi.Services;

public interface IKafkaProducerService
{
    Task PublishInvoiceCreatedEventAsync(int invoiceId);
    Task PublishQuoteCreatedEventAsync(int quoteId);
    Task PublishQuoteApprovalRequestedEventAsync(int quoteId, int workflowId);
    Task PublishInvoiceGeneratedEventAsync(int invoiceId, int workflowId);
}
