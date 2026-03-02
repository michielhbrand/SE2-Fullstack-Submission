using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;
using InvoiceTrackerApi.Services.Invoice;

namespace InvoiceTrackerApi.Services.Workflow;

/// <summary>
/// Leaf service that converts a quote to an invoice.
/// Depends on IInvoiceService directly — no circular dependency since
/// IInvoiceService → IWorkflowService, and this service is not IWorkflowService.
/// </summary>
public class QuoteToInvoiceConversionService : IQuoteToInvoiceConversionService
{
    private readonly IInvoiceService _invoiceService;

    public QuoteToInvoiceConversionService(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public Task<InvoiceResponse> ConvertAsync(int quoteId, int? payByDays, string userId, int organizationId, CancellationToken ct = default)
    {
        return _invoiceService.ConvertQuoteToInvoiceAsync(
            new ConvertQuoteToInvoiceRequest
            {
                QuoteId    = quoteId,
                PayByDays  = payByDays ?? 30
            },
            userId,
            organizationId);
    }
}
