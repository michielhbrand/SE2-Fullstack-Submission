using Shared.Database.Data;
using Microsoft.EntityFrameworkCore;
using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Repositories.Workflow;

/// <summary>
/// Repository implementation for Workflow data access
/// </summary>
public class WorkflowRepository : Repository<WorkflowModel>, IWorkflowRepository
{
    public WorkflowRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<WorkflowModel?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Workflows
            .Include(w => w.Events.OrderByDescending(e => e.OccurredAt))
            .Include(w => w.Client)
            .Include(w => w.Quote)
            .Include(w => w.Invoice)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<IEnumerable<WorkflowModel>> GetAllByOrganizationAsync(int organizationId, int page, int pageSize, string? search = null, List<string>? statuses = null)
    {
        var query = _context.Workflows
            .Include(w => w.Client)
            .Where(w => w.OrganizationId == organizationId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(w =>
                (w.Client != null && w.Client.Name.ToLower().Contains(s)) ||
                (w.Client != null && w.Client.Email != null && w.Client.Email.ToLower().Contains(s)));
        }

        if (statuses != null && statuses.Count > 0)
            query = query.Where(w => statuses.Contains(w.Status));

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountByOrganizationAsync(int organizationId, string? search = null, List<string>? statuses = null)
    {
        var query = _context.Workflows
            .Include(w => w.Client)
            .Where(w => w.OrganizationId == organizationId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(w =>
                (w.Client != null && w.Client.Name.ToLower().Contains(s)) ||
                (w.Client != null && w.Client.Email != null && w.Client.Email.ToLower().Contains(s)));
        }

        if (statuses != null && statuses.Count > 0)
            query = query.Where(w => statuses.Contains(w.Status));

        return await query.CountAsync();
    }

    public async Task<WorkflowModel?> GetByQuoteIdAsync(int quoteId)
    {
        return await _context.Workflows
            .Include(w => w.Events.OrderByDescending(e => e.OccurredAt))
            .Include(w => w.Client)
            .FirstOrDefaultAsync(w => w.QuoteId == quoteId);
    }

    public async Task<WorkflowModel?> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _context.Workflows
            .Include(w => w.Events.OrderByDescending(e => e.OccurredAt))
            .Include(w => w.Client)
            .FirstOrDefaultAsync(w => w.InvoiceId == invoiceId);
    }
}
