using InvoiceTrackerApi.DTOs.Dashboard;

namespace InvoiceTrackerApi.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(int organizationId);
}
