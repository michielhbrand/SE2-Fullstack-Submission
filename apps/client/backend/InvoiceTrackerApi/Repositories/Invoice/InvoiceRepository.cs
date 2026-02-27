using Shared.Database.Data;
using Shared.Database.Models;
using Microsoft.EntityFrameworkCore;
using InvoiceModel = Shared.Database.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository implementation for Invoice data access
/// </summary>
public class InvoiceRepository : Repository<InvoiceModel>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<InvoiceModel?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InvoiceModel>> GetAllAsync(int organizationId, int page, int pageSize, bool overdueOnly = false)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .Where(i => i.OrganizationId == organizationId);

        if (overdueOnly)
        {
            var now = DateTime.UtcNow;
            var blocked = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };
            query = query
                .Where(i => i.PayByDate < now)
                .Where(i => _context.Workflows.Any(w => w.InvoiceId == i.Id && !blocked.Contains(w.Status)));
        }

        return await query
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(int organizationId, bool overdueOnly = false)
    {
        var query = _context.Invoices
            .Where(i => i.OrganizationId == organizationId);

        if (overdueOnly)
        {
            var now = DateTime.UtcNow;
            var blocked = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };
            query = query
                .Where(i => i.PayByDate < now)
                .Where(i => _context.Workflows.Any(w => w.InvoiceId == i.Id && !blocked.Contains(w.Status)));
        }

        return await query.CountAsync();
    }

    public async Task<IEnumerable<(InvoiceModel Invoice, int WorkflowId)>> GetOverdueAsync(
        DateTime cutoff, int reminderIntervalDays, int? organizationId = null, CancellationToken ct = default)
    {
        var reminderCutoff = DateTime.UtcNow.AddDays(-reminderIntervalDays);
        var blocked = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };

        var workflowQuery = _context.Workflows
            .Where(w => w.InvoiceId != null
                     && !blocked.Contains(w.Status)
                     && w.Invoice!.PayByDate < cutoff
                     && !w.Events.Any(we =>
                            we.EventType == WorkflowEventType.OverdueReminderSent
                            && we.OccurredAt > reminderCutoff));

        if (organizationId.HasValue)
            workflowQuery = workflowQuery.Where(w => w.OrganizationId == organizationId.Value);

        var pairs = await workflowQuery
            .Select(w => new { InvoiceId = w.InvoiceId!.Value, WorkflowId = w.Id })
            .ToListAsync(ct);

        if (!pairs.Any())
            return [];

        var invoiceIds = pairs.Select(p => p.InvoiceId).ToList();

        var invoices = await _context.Invoices
            .Include(i => i.Client)
            .Where(i => invoiceIds.Contains(i.Id))
            .ToListAsync(ct);

        return pairs
            .Join(invoices, p => p.InvoiceId, inv => inv.Id, (p, inv) => (inv, p.WorkflowId))
            .ToList();
    }
}
