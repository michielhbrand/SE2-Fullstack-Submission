namespace InvoiceTrackerApi.DTOs.Dashboard;

public class DashboardResponse
{
    public DashboardKpis Kpis { get; set; } = new();
    public List<MonthlyRevenue> RevenueByMonth { get; set; } = new();
    public InvoiceStatusBreakdown InvoiceStatusBreakdown { get; set; } = new();
    public List<WorkflowStatusCount> WorkflowStatusBreakdown { get; set; } = new();
    public List<TopClient> TopClients { get; set; } = new();
    public List<RecentActivityItem> RecentActivity { get; set; } = new();
    public List<OverdueInvoiceItem> OverdueInvoices { get; set; } = new();
}

public class DashboardKpis
{
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int ActiveWorkflows { get; set; }
    public int TotalInvoices { get; set; }
    public int TotalClients { get; set; }
}

public class MonthlyRevenue
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class InvoiceStatusBreakdown
{
    public int Paid { get; set; }
    public int Overdue { get; set; }
    public int NotPaid { get; set; }
}

public class WorkflowStatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopClient
{
    public string ClientName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

public class RecentActivityItem
{
    public int WorkflowId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public class OverdueInvoiceItem
{
    public int InvoiceId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime PayByDate { get; set; }
}
