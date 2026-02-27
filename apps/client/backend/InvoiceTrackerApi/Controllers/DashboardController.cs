using InvoiceTrackerApi.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get dashboard summary data for an organisation
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int organizationId)
    {
        var result = await _dashboardService.GetDashboardAsync(organizationId);
        return Ok(result);
    }
}
