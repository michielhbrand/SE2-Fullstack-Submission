using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Repositories.Workflow;

/// <summary>
/// Repository interface for Workflow data access
/// </summary>
public interface IWorkflowRepository : IRepository<WorkflowModel>
{
    Task<WorkflowModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<WorkflowModel>> GetAllByOrganizationAsync(int organizationId, int page, int pageSize, string? search = null, List<string>? statuses = null);
    Task<int> GetTotalCountByOrganizationAsync(int organizationId, string? search = null, List<string>? statuses = null);
    Task<WorkflowModel?> GetByQuoteIdAsync(int quoteId);
    Task<WorkflowModel?> GetByInvoiceIdAsync(int invoiceId);
}
