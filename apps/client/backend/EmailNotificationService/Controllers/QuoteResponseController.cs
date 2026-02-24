using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using Shared.Database.Models;
using EmailNotificationService.Services;

namespace EmailNotificationService.Controllers;

/// <summary>
/// Handles token-based quote approval/rejection responses from email links.
/// These endpoints are unauthenticated — security is provided by the HMAC-signed token.
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

    /// <summary>
    /// Approve a quote via email token link
    /// </summary>
    [HttpGet("approve")]
    [Produces("text/html")]
    public async Task<IActionResult> ApproveQuote([FromQuery] string token)
    {
        return await ProcessQuoteResponse(token, "approve");
    }

    /// <summary>
    /// Reject a quote via email token link
    /// </summary>
    [HttpGet("reject")]
    [Produces("text/html")]
    public async Task<IActionResult> RejectQuote([FromQuery] string token)
    {
        return await ProcessQuoteResponse(token, "reject");
    }

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

            // Find the workflow
            var workflow = await dbContext.Workflows
                .Include(w => w.Events)
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (workflow == null)
            {
                return Content(
                    EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId, false, "Workflow not found."),
                    "text/html");
            }

            // Check workflow is in PendingApproval status
            if (workflow.Status != WorkflowStatus.PendingApproval)
            {
                return Content(
                    EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId, false,
                        $"This quote has already been processed. Current status: {workflow.Status}"),
                    "text/html");
            }

            // Determine the event type based on action
            var eventType = action.Equals("approve", StringComparison.OrdinalIgnoreCase)
                ? WorkflowEventType.Approved
                : WorkflowEventType.Rejected;

            var newStatus = action.Equals("approve", StringComparison.OrdinalIgnoreCase)
                ? WorkflowStatus.Approved
                : WorkflowStatus.Rejected;

            // Add the event
            var workflowEvent = new WorkflowEvent
            {
                WorkflowId = workflowId,
                EventType = eventType,
                Description = $"Quote {action}d via email link by client",
                PerformedBy = "Client (email)",
                OccurredAt = DateTime.UtcNow
            };

            dbContext.WorkflowEvents.Add(workflowEvent);

            // Update workflow status
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
}
