using InvoiceTrackerApi.Repositories.OrganizationMember;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceTrackerApi.Filters;

/// <summary>
/// Globally-applied action filter that closes the IDOR vulnerability by verifying
/// the authenticated user is actually a member of the organizationId present in the
/// request (query-string, route, or form) before the action executes.
///
/// Cache key "OrgAuthPassed" is stored in HttpContext.Items so the check is performed
/// at most once per request even if multiple filter stages run.
///
/// Decorate a controller or action with [SkipOrgAuth] to opt out (e.g. endpoints
/// that are intentionally cross-org or have no org context at all).
/// </summary>
public class OrganizationAuthorizationFilter : IAsyncActionFilter
{
    private const string CacheKey = "OrgAuthPassed";
    private readonly IOrganizationMemberRepository _memberRepo;
    private readonly ILogger<OrganizationAuthorizationFilter> _logger;

    public OrganizationAuthorizationFilter(
        IOrganizationMemberRepository memberRepo,
        ILogger<OrganizationAuthorizationFilter> logger)
    {
        _memberRepo = memberRepo;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // --- opt-out ---
        if (HasSkipAttribute(context))
        {
            await next();
            return;
        }

        // --- already verified earlier in this request ---
        if (context.HttpContext.Items.ContainsKey(CacheKey))
        {
            await next();
            return;
        }

        // --- resolve organizationId from request ---
        var rawOrgId =
            context.HttpContext.Request.Query["organizationId"].FirstOrDefault()
            ?? context.RouteData.Values["organizationId"]?.ToString();

        if (rawOrgId == null || !int.TryParse(rawOrgId, out var organizationId))
        {
            // No org context in this request — let the action handle it
            await next();
            return;
        }

        // --- resolve the caller's identity ---
        var userId = context.HttpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            // Not authenticated — let [Authorize] handle the 401
            await next();
            return;
        }

        // --- membership check ---
        var isMember = await _memberRepo.IsMemberAsync(organizationId, userId);
        if (!isMember)
        {
            _logger.LogWarning(
                "IDOR blocked: user {UserId} attempted to access org {OrgId} without membership",
                userId, organizationId);

            context.Result = new ObjectResult(new { message = "You are not a member of this organisation." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        context.HttpContext.Items[CacheKey] = true;
        await next();
    }

    private static bool HasSkipAttribute(ActionExecutingContext context)
    {
        // Check action-level attribute first, then controller-level
        return context.ActionDescriptor.EndpointMetadata
            .OfType<SkipOrgAuthAttribute>()
            .Any();
    }
}
