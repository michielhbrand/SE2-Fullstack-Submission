namespace EmailNotificationService.Services;

/// <summary>
/// Service for sending emails via SMTP using MailKit
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a quote approval request email with Accept/Reject buttons and optional PDF attachment
    /// </summary>
    Task SendQuoteApprovalEmailAsync(string toEmail, string toName, int quoteId, int workflowId, string approveToken, string rejectToken, byte[]? pdfAttachment = null);

    /// <summary>
    /// Send an invoice payment request email with optional PDF attachment
    /// </summary>
    Task SendInvoiceGeneratedEmailAsync(string toEmail, string toName, int invoiceId, int workflowId, byte[]? pdfAttachment = null);

    /// <summary>
    /// Send an overdue invoice reminder email with optional PDF attachment
    /// </summary>
    Task SendOverdueInvoiceEmailAsync(string toEmail, string toName, int invoiceId, int workflowId, DateTime payByDate, byte[]? pdfAttachment = null);
}
