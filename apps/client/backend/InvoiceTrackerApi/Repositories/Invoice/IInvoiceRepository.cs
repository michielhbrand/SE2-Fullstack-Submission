using InvoiceModel = Shared.Database.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository interface for Invoice data access
/// </summary>
public interface IInvoiceRepository : IRepository<InvoiceModel>
{
    Task<InvoiceModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<InvoiceModel>> GetAllAsync(int organizationId, int page, int pageSize, bool overdueOnly = false);
    Task<int> GetTotalCountAsync(int organizationId, bool overdueOnly = false);
    Task<IEnumerable<(InvoiceModel Invoice, int WorkflowId)>> GetOverdueAsync(
        DateTime cutoff, int reminderIntervalDays, int? organizationId = null, CancellationToken ct = default);
}
