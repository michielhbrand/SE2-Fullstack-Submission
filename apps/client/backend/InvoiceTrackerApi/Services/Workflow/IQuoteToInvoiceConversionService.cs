using InvoiceTrackerApi.DTOs.Invoice.Responses;

namespace InvoiceTrackerApi.Services.Workflow;

public interface IQuoteToInvoiceConversionService
{
    Task<InvoiceResponse> ConvertAsync(int quoteId, int? payByDays, string userId, int organizationId, CancellationToken ct = default);
}
