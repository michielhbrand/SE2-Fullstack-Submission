using InvoiceTrackerApi.DTOs;
using InvoiceTrackerApi.Models;
using InvoiceModel = InvoiceTrackerApi.Models.Invoice;

namespace InvoiceTrackerApi.Services.Invoice;

/// <summary>
/// Service interface for Invoice business logic
/// </summary>
public interface IInvoiceService
{
    Task<PaginatedResponse<InvoiceModel>> GetInvoicesAsync(int page, int pageSize);
    Task<InvoiceModel> GetInvoiceByIdAsync(int id);
    Task<InvoiceModel> CreateInvoiceAsync(CreateInvoiceRequest request, string modifiedBy);
    Task<InvoiceModel> UpdateInvoiceAsync(int id, UpdateInvoiceRequest request, string modifiedBy);
    Task DeleteInvoiceAsync(int id);
    Task<string?> GetInvoicePdfUrlAsync(int id);
}
