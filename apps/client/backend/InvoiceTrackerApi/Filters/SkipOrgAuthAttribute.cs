namespace InvoiceTrackerApi.Filters;

/// <summary>
/// Opt-out marker for OrganizationAuthorizationFilter.
/// Apply to any action or controller that legitimately has no org context
/// (e.g. health checks, user-profile endpoints).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipOrgAuthAttribute : Attribute { }
