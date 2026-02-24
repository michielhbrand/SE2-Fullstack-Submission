namespace EmailNotificationService.Services;

/// <summary>
/// Service for sending emails via SMTP using MailKit
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a quote approval request email with Accept/Reject buttons
    /// </summary>
    Task SendQuoteApprovalEmailAsync(string toEmail, string toName, int quoteId, int workflowId, string approveToken, string rejectToken);

    /// <summary>
    /// Send an invoice generated notification email
    /// </summary>
    Task SendInvoiceGeneratedEmailAsync(string toEmail, string toName, int invoiceId, int workflowId);
}
