using InvoiceModel = InvoiceTrackerApi.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository interface for Invoice data access
/// </summary>
public interface IInvoiceRepository : IRepository<InvoiceModel>
{
    Task<InvoiceModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<InvoiceModel>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
}
