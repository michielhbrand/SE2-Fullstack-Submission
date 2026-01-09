namespace AuthApi.Services;

public interface IKafkaProducerService
{
    Task PublishInvoiceCreatedEventAsync(int invoiceId);
    Task PublishQuoteCreatedEventAsync(int quoteId);
}
