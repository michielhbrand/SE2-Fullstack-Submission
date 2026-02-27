using InvoiceModel = Shared.Database.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository interface for Invoice data access
/// </summary>
public interface IInvoiceRepository : IRepository<InvoiceModel>
{
    Task<InvoiceModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<(InvoiceModel Invoice, string? WorkflowStatus)>> GetAllAsync(
        int organizationId, int page, int pageSize,
        string? statusFilter = null, string? search = null);
    Task<int> GetTotalCountAsync(int organizationId, string? statusFilter = null, string? search = null);
    Task<IEnumerable<(InvoiceModel Invoice, int WorkflowId)>> GetOverdueAsync(
        DateTime cutoff, int reminderIntervalDays, int? organizationId = null, CancellationToken ct = default);
}
