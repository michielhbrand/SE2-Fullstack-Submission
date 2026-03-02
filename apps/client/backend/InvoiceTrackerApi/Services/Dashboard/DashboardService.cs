using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using Shared.Database.Models;
using InvoiceTrackerApi.DTOs.Dashboard;

namespace InvoiceTrackerApi.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, TimeProvider timeProvider, ILogger<DashboardService> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<DashboardResponse> GetDashboardAsync(int organizationId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var kpis                    = await GetKpiDataAsync(organizationId, now);
        var statusBreakdown         = await GetStatusBreakdownAsync(organizationId, now);
        var revenueByMonth          = await GetRevenueByMonthAsync(organizationId, now);
        var topClients              = await GetTopClientsAsync(organizationId);
        var recentActivity          = await GetRecentActivityAsync(organizationId);
        var overdueInvoices         = await GetOverdueInvoicesAsync(organizationId, now);

        _logger.LogInformation(
            "Dashboard loaded for OrgId {OrgId}: revenue={Revenue:C}, outstanding={Outstanding:C}, overdue={Overdue}",
            organizationId, kpis.TotalRevenue, kpis.OutstandingAmount, overdueInvoices.Count);

        return new DashboardResponse
        {
            Kpis                    = kpis,
            RevenueByMonth          = revenueByMonth,
            InvoiceStatusBreakdown  = statusBreakdown.InvoiceStatus,
            WorkflowStatusBreakdown = statusBreakdown.WorkflowStatus,
            TopClients              = topClients,
            RecentActivity          = recentActivity,
            OverdueInvoices         = overdueInvoices
        };
    }

    private async Task<DashboardKpis> GetKpiDataAsync(int organizationId, DateTime now)
    {
        var terminal = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };

        var workflowData = await _context.Workflows
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId && w.InvoiceId != null)
            .Select(w => new { w.Status, InvoiceId = w.InvoiceId!.Value, PayByDate = w.Invoice!.PayByDate })
            .ToListAsync();

        var allInvoiceIds = workflowData.Select(x => x.InvoiceId).Distinct().ToList();
        var allInvoiceTotals = allInvoiceIds.Any()
            ? (await _context.InvoiceItems
                .AsNoTracking()
                .Where(i => allInvoiceIds.Contains(i.InvoiceId))
                .GroupBy(i => i.InvoiceId)
                .Select(g => new { InvoiceId = g.Key, Total = g.Sum(i => i.Quantity * i.PricePerUnit) })
                .ToListAsync())
                .ToDictionary(x => x.InvoiceId, x => x.Total)
            : new Dictionary<int, decimal>();

        var totalRevenue = workflowData
            .Where(x => x.Status == WorkflowStatus.Paid)
            .Sum(x => allInvoiceTotals.GetValueOrDefault(x.InvoiceId));

        var outstandingAmount = workflowData
            .Where(x => !terminal.Contains(x.Status))
            .Sum(x => allInvoiceTotals.GetValueOrDefault(x.InvoiceId));

        var overdueAmount = workflowData
            .Where(x => !terminal.Contains(x.Status) && x.PayByDate < now)
            .Sum(x => allInvoiceTotals.GetValueOrDefault(x.InvoiceId));

        var activeWorkflows = await _context.Workflows
            .AsNoTracking()
            .CountAsync(w => w.OrganizationId == organizationId && !terminal.Contains(w.Status));

        var totalInvoices = await _context.Invoices
            .AsNoTracking()
            .CountAsync(i => i.OrganizationId == organizationId);

        var totalClients = await _context.Clients
            .AsNoTracking()
            .CountAsync(c => c.OrganizationId == organizationId);

        return new DashboardKpis
        {
            TotalRevenue      = totalRevenue,
            OutstandingAmount = outstandingAmount,
            OverdueAmount     = overdueAmount,
            ActiveWorkflows   = activeWorkflows,
            TotalInvoices     = totalInvoices,
            TotalClients      = totalClients
        };
    }

    private async Task<(InvoiceStatusBreakdown InvoiceStatus, List<WorkflowStatusCount> WorkflowStatus)>
        GetStatusBreakdownAsync(int organizationId, DateTime now)
    {
        var terminal = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };

        var workflowData = await _context.Workflows
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId && w.InvoiceId != null)
            .Select(w => new { w.Status, PayByDate = w.Invoice!.PayByDate })
            .ToListAsync();

        var invoiceStatusBreakdown = new InvoiceStatusBreakdown
        {
            Paid    = workflowData.Count(x => x.Status == WorkflowStatus.Paid),
            Overdue = workflowData.Count(x => !terminal.Contains(x.Status) && x.PayByDate < now),
            NotPaid = workflowData.Count(x => !terminal.Contains(x.Status) && x.PayByDate >= now)
        };

        var workflowStatusBreakdown = await _context.Workflows
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId)
            .GroupBy(w => w.Status)
            .Select(g => new WorkflowStatusCount { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return (invoiceStatusBreakdown, workflowStatusBreakdown);
    }

    private async Task<List<MonthlyRevenue>> GetRevenueByMonthAsync(int organizationId, DateTime now)
    {
        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        var paidEvents = await _context.WorkflowEvents
            .AsNoTracking()
            .Where(we => we.EventType == WorkflowEventType.MarkedAsPaid
                      && we.OccurredAt >= sixMonthsAgo
                      && we.Workflow!.OrganizationId == organizationId
                      && we.Workflow.InvoiceId != null)
            .Select(we => new
            {
                Month     = new DateTime(we.OccurredAt.Year, we.OccurredAt.Month, 1),
                InvoiceId = we.Workflow!.InvoiceId!.Value
            })
            .ToListAsync();

        var paidEventInvoiceIds = paidEvents.Select(x => x.InvoiceId).Distinct().ToList();
        var paidTotalsMap = paidEventInvoiceIds.Any()
            ? (await _context.InvoiceItems
                .AsNoTracking()
                .Where(i => paidEventInvoiceIds.Contains(i.InvoiceId))
                .GroupBy(i => i.InvoiceId)
                .Select(g => new { InvoiceId = g.Key, Total = g.Sum(i => i.Quantity * i.PricePerUnit) })
                .ToListAsync())
                .ToDictionary(x => x.InvoiceId, x => x.Total)
            : new Dictionary<int, decimal>();

        return Enumerable.Range(0, 6)
            .Select(offset => sixMonthsAgo.AddMonths(offset))
            .Select(m => new MonthlyRevenue
            {
                Month  = m.ToString("MMM yy"),
                Amount = paidEvents
                    .Where(e => e.Month == m)
                    .Sum(e => paidTotalsMap.GetValueOrDefault(e.InvoiceId))
            })
            .ToList();
    }

    private async Task<List<TopClient>> GetTopClientsAsync(int organizationId)
    {
        var paidWorkflowClients = await _context.Workflows
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId
                     && w.Status == WorkflowStatus.Paid
                     && w.InvoiceId != null)
            .Select(w => new { ClientName = w.Client!.Name, InvoiceId = w.InvoiceId!.Value })
            .ToListAsync();

        var clientInvoiceIds = paidWorkflowClients.Select(x => x.InvoiceId).Distinct().ToList();
        var clientTotalsMap = clientInvoiceIds.Any()
            ? (await _context.InvoiceItems
                .AsNoTracking()
                .Where(i => clientInvoiceIds.Contains(i.InvoiceId))
                .GroupBy(i => i.InvoiceId)
                .Select(g => new { InvoiceId = g.Key, Total = g.Sum(i => i.Quantity * i.PricePerUnit) })
                .ToListAsync())
                .ToDictionary(x => x.InvoiceId, x => x.Total)
            : new Dictionary<int, decimal>();

        return paidWorkflowClients
            .GroupBy(x => x.ClientName)
            .Select(g => new TopClient
            {
                ClientName   = g.Key,
                Revenue      = g.Sum(x => clientTotalsMap.GetValueOrDefault(x.InvoiceId)),
                InvoiceCount = g.Count()
            })
            .OrderByDescending(c => c.Revenue)
            .Take(5)
            .ToList();
    }

    private async Task<List<RecentActivityItem>> GetRecentActivityAsync(int organizationId)
    {
        return await _context.WorkflowEvents
            .AsNoTracking()
            .Where(we => we.Workflow!.OrganizationId == organizationId)
            .OrderByDescending(we => we.OccurredAt)
            .Take(8)
            .Select(we => new RecentActivityItem
            {
                WorkflowId = we.WorkflowId,
                ClientName = we.Workflow!.Client!.Name,
                EventType  = we.EventType,
                OccurredAt = we.OccurredAt
            })
            .ToListAsync();
    }

    private async Task<List<OverdueInvoiceItem>> GetOverdueInvoicesAsync(int organizationId, DateTime now)
    {
        var terminal = new[] { WorkflowStatus.Paid, WorkflowStatus.Cancelled, WorkflowStatus.Terminated };

        var overdueWorkflows = await _context.Workflows
            .AsNoTracking()
            .Where(w => w.OrganizationId == organizationId
                     && w.InvoiceId != null
                     && !terminal.Contains(w.Status)
                     && w.Invoice!.PayByDate < now)
            .Select(w => new
            {
                InvoiceId  = w.InvoiceId!.Value,
                ClientName = w.Client!.Name,
                PayByDate  = w.Invoice!.PayByDate
            })
            .OrderBy(x => x.PayByDate)
            .Take(6)
            .ToListAsync();

        var overdueInvoiceIds = overdueWorkflows.Select(x => x.InvoiceId).ToList();
        var overdueTotalsMap = overdueInvoiceIds.Any()
            ? (await _context.InvoiceItems
                .AsNoTracking()
                .Where(i => overdueInvoiceIds.Contains(i.InvoiceId))
                .GroupBy(i => i.InvoiceId)
                .Select(g => new { InvoiceId = g.Key, Total = g.Sum(i => i.Quantity * i.PricePerUnit) })
                .ToListAsync())
                .ToDictionary(x => x.InvoiceId, x => x.Total)
            : new Dictionary<int, decimal>();

        return overdueWorkflows
            .Select(x => new OverdueInvoiceItem
            {
                InvoiceId   = x.InvoiceId,
                ClientName  = x.ClientName,
                Amount      = overdueTotalsMap.GetValueOrDefault(x.InvoiceId),
                DaysOverdue = (int)(now - x.PayByDate).TotalDays,
                PayByDate   = x.PayByDate
            })
            .OrderByDescending(x => x.DaysOverdue)
            .ToList();
    }
}
