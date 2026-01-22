using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;

namespace InvoiceTrackerApi.Services.Invoice;

/// <summary>
/// Service interface for Invoice business logic
/// </summary>
public interface IInvoiceService
{
    Task<PaginatedResponse<InvoiceResponse>> GetInvoicesAsync(int page, int pageSize);
    Task<InvoiceResponse> GetInvoiceByIdAsync(int id);
    Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, string modifiedBy);
    Task<InvoiceResponse> UpdateInvoiceAsync(int id, UpdateInvoiceRequest request, string modifiedBy);
    Task DeleteInvoiceAsync(int id);
    Task<string?> GetInvoicePdfUrlAsync(int id);
}
