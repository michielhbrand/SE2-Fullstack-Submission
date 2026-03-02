using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using Shared.Database.Models;
using EmailNotificationService.Services;

namespace EmailNotificationService.Controllers;

/// <summary>
/// Handles token-based quote approval/rejection responses from email links.
/// These endpoints are unauthenticated — security is provided by the HMAC-signed token.
///
/// GET  → renders a confirmation page (prevents prefetch / accidental activation).
/// POST → performs the actual state change after the user explicitly confirms.
/// </summary>
[ApiController]
[Route("api/quote-response")]
public class QuoteResponseController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuoteResponseController> _logger;

    public QuoteResponseController(
        ITokenService tokenService,
        IServiceProvider serviceProvider,
        ILogger<QuoteResponseController> logger)
    {
        _tokenService = tokenService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // ── Confirmation pages (GET) ───────────────────────────────────────────────

    /// <summary>
    /// Shows a confirmation page for approving a quote.
    /// The user must click the confirm button to perform the actual state change.
    /// </summary>
    [HttpGet("approve")]
    [Produces("text/html")]
    public IActionResult ApproveConfirmation([FromQuery] string token)
        => Content(BuildConfirmationPage("approve", token), "text/html");

    /// <summary>
    /// Shows a confirmation page for rejecting a quote.
    /// </summary>
    [HttpGet("reject")]
    [Produces("text/html")]
    public IActionResult RejectConfirmation([FromQuery] string token)
        => Content(BuildConfirmationPage("reject", token), "text/html");

    // ── Action endpoints (POST) ────────────────────────────────────────────────

    /// <summary>
    /// Approves a quote after the user confirms via the GET confirmation page.
    /// </summary>
    [HttpPost("approve")]
    [Produces("text/html")]
    public async Task<IActionResult> ApproveQuote([FromQuery] string token)
        => await ProcessQuoteResponse(token, "approve");

    /// <summary>
    /// Rejects a quote after the user confirms via the GET confirmation page.
    /// </summary>
    [HttpPost("reject")]
    [Produces("text/html")]
    public async Task<IActionResult> RejectQuote([FromQuery] string token)
        => await ProcessQuoteResponse(token, "reject");

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ProcessQuoteResponse(string token, string expectedAction)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Content(
                EmailTemplates.GetQuoteResponseConfirmationHtml(expectedAction, 0, false, "Missing token."),
                "text/html");
        }

        var result = _tokenService.ValidateToken(token);

        if (result == null)
        {
            return Content(
                EmailTemplates.GetQuoteResponseConfirmationHtml(expectedAction, 0, false, "Invalid or expired token."),
                "text/html");
        }

        var (workflowId, quoteId, action) = result.Value;

        if (!action.Equals(expectedAction, StringComparison.OrdinalIgnoreCase))
        {
            return Content(
                EmailTemplates.GetQuoteResponseConfirmationHtml(expectedAction, quoteId, false, "Token action mismatch."),
                "text/html");
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var workflow = await dbContext.Workflows
                .Include(w => w.Events)
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (workflow == null)
            {
                return Content(
                    EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId, false, "Workflow not found."),
                    "text/html");
            }

            if (workflow.Status != WorkflowStatus.PendingApproval)
            {
                return Content(
                    EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId, false,
                        $"This quote has already been processed. Current status: {workflow.Status}"),
                    "text/html");
            }

            var eventType = action.Equals("approve", StringComparison.OrdinalIgnoreCase)
                ? WorkflowEventType.Approved
                : WorkflowEventType.Rejected;

            var newStatus = action.Equals("approve", StringComparison.OrdinalIgnoreCase)
                ? WorkflowStatus.Approved
                : WorkflowStatus.Rejected;

            dbContext.WorkflowEvents.Add(new WorkflowEvent
            {
                WorkflowId = workflowId,
                EventType = eventType,
                Description = $"Quote {action}d via email link by client",
                PerformedBy = "Client (email)",
                OccurredAt = DateTime.UtcNow
            });

            workflow.Status = newStatus;
            workflow.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Quote #{QuoteId} {Action}d via email token. Workflow #{WorkflowId} status updated to {Status}",
                quoteId, action, workflowId, newStatus);

            return Content(
                EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId, true),
                "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quote response for Workflow #{WorkflowId}", workflowId);
            return Content(
                EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId, false,
                    "An unexpected error occurred. Please try again or contact support."),
                "text/html");
        }
    }

    private static string BuildConfirmationPage(string action, string token)
    {
        var verb = action == "approve" ? "Approve" : "Reject";
        var colour = action == "approve" ? "#16a34a" : "#dc2626";
        var postUrl = $"/api/quote-response/{action}?token={Uri.EscapeDataString(token ?? "")}";

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>Confirm {{verb}}</title>
              <style>
                body { font-family: sans-serif; display: flex; align-items: center;
                        justify-content: center; min-height: 100vh; margin: 0;
                        background: #f9fafb; }
                .card { background: #fff; border-radius: 8px; padding: 2rem 2.5rem;
                          box-shadow: 0 2px 12px rgba(0,0,0,.1); max-width: 420px;
                          text-align: center; }
                h1 { font-size: 1.4rem; margin-bottom: .5rem; }
                p  { color: #6b7280; margin-bottom: 1.5rem; }
                button { background: {{colour}}; color: #fff; border: none;
                           border-radius: 6px; padding: .75rem 2rem;
                           font-size: 1rem; cursor: pointer; }
                button:hover { opacity: .9; }
              </style>
            </head>
            <body>
              <div class="card">
                <h1>Confirm {{verb}}</h1>
                <p>Please confirm that you want to <strong>{{action}}</strong> this quote.</p>
                <form method="post" action="{{postUrl}}">
                  <button type="submit">{{verb}} Quote</button>
                </form>
              </div>
            </body>
            </html>
            """;
    }
}
