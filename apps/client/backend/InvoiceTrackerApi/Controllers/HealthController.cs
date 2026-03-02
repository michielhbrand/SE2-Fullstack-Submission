using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly TimeProvider _timeProvider;

    public HealthController(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = _timeProvider.GetUtcNow().UtcDateTime,
            service = "InvoiceTrackerApi"
        });
    }
}
