using Microsoft.AspNetCore.Mvc;
using Shared.Core.Exceptions.Application;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Base controller for authenticated endpoints that require user identity extraction
/// </summary>
public abstract class AuthenticatedControllerBase : ControllerBase
{
    /// <summary>
    /// Extracts the current user's identifier from JWT claims
    /// </summary>
    /// <returns>User identifier (email or username)</returns>
    /// <exception cref="UnauthorizedException">Thrown when user identity cannot be determined</exception>
    protected string GetCurrentUserIdentifier()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new UnauthorizedException("User identity not found in token claims");
        }

        return userEmail;
    }

    /// <summary>
    /// Extracts the Keycloak user ID (sub claim) from JWT token.
    /// This is the authoritative user identifier for organization membership.
    /// </summary>
    /// <returns>Keycloak user ID (sub claim)</returns>
    /// <exception cref="UnauthorizedException">Thrown when sub claim cannot be found</exception>
    protected string GetCurrentUserId()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("User ID (sub claim) not found in token claims");
        }

        return userId;
    }
}
