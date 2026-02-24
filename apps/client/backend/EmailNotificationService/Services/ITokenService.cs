namespace EmailNotificationService.Services;

/// <summary>
/// Service for generating and validating token-based quote response links
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a unique token for a quote response action
    /// </summary>
    string GenerateToken(int workflowId, int quoteId, string action);

    /// <summary>
    /// Validate and decode a token, returning the workflow ID, quote ID, and action
    /// </summary>
    (int WorkflowId, int QuoteId, string Action)? ValidateToken(string token);
}
