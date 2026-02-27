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

    public async Task<IEnumerable<(InvoiceModel Invoice, string? WorkflowStatus)>> GetAllAsync(
        int organizationId, int page, int pageSize,
        string? statusFilter = null, string? search = null)
    {
        var baseQuery = BuildFilteredQuery(organizationId, statusFilter, search);

        // Step 1: fetch IDs + workflow statuses (projection avoids Include conflict)
        var pairs = await baseQuery
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                InvoiceId = i.Id,
                WorkflowStatus = _context.Workflows
                    .Where(w => w.InvoiceId == i.Id)
                    .Select(w => w.Status)
                    .FirstOrDefault()
            })
            .ToListAsync();

        if (!pairs.Any())
            return [];

        // Step 2: load full invoice data with navigation properties
        var ids = pairs.Select(p => p.InvoiceId).ToList();
        var invoices = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .Where(i => ids.Contains(i.Id))
            .ToListAsync();

        // Step 3: rejoin preserving original order
        return pairs
            .Join(invoices, p => p.InvoiceId, inv => inv.Id,
                  (p, inv) => (inv, (string?)p.WorkflowStatus))
            .ToList();
    }

    public async Task<int> GetTotalCountAsync(int organizationId, string? statusFilter = null, string? search = null)
    {
        return await BuildFilteredQuery(organizationId, statusFilter, search).CountAsync();
    }

    private IQueryable<InvoiceModel> BuildFilteredQuery(int organizationId, string? statusFilter, string? search)
    {
        var now = DateTime.UtcNow;
        var terminal = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };

        var query = _context.Invoices
            .Where(i => i.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.Client!.Name.ToLower().Contains(search.ToLower()));

        query = statusFilter switch
        {
            "Paid" => query.Where(i =>
                _context.Workflows.Any(w => w.InvoiceId == i.Id && w.Status == WorkflowStatus.Paid)),
            "Overdue" => query
                .Where(i => i.PayByDate < now)
                .Where(i => _context.Workflows.Any(w => w.InvoiceId == i.Id && !terminal.Contains(w.Status))),
            "NotPaid" => query
                .Where(i => i.PayByDate >= now)
                .Where(i => _context.Workflows.Any(w => w.InvoiceId == i.Id && !terminal.Contains(w.Status))),
            _ => query
        };

        return query;
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
