using InvoiceTrackerApi.Models;
using InvoiceModel = InvoiceTrackerApi.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository interface for Invoice data access
/// </summary>
public interface IInvoiceRepository
{
    Task<InvoiceModel?> GetByIdAsync(int id);
    Task<InvoiceModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<InvoiceModel>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<InvoiceModel> AddAsync(InvoiceModel invoice);
    Task UpdateAsync(InvoiceModel invoice);
    Task DeleteAsync(InvoiceModel invoice);
    Task<bool> ExistsAsync(int id);
}
