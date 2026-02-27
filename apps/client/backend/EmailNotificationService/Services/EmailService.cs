using MailKit.Net.Smtp;
using MimeKit;

namespace EmailNotificationService.Services;

/// <summary>
/// MailKit-based email service for sending SMTP emails.
/// Uses MailHog (localhost:1025) in development for email capture.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendQuoteApprovalEmailAsync(
        string toEmail, string toName, int quoteId, int workflowId,
        string approveToken, string rejectToken, byte[]? pdfAttachment = null)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5002";

        var approveUrl = $"{baseUrl}/api/quote-response/approve?token={approveToken}";
        var rejectUrl = $"{baseUrl}/api/quote-response/reject?token={rejectToken}";

        var htmlBody = EmailTemplates.GetQuoteApprovalTemplate(
            toName, quoteId, workflowId, approveUrl, rejectUrl);

        var subject = $"Quote #{quoteId} — Approval Required";

        await SendEmailAsync(toEmail, toName, subject, htmlBody,
            pdfAttachment != null ? ($"Quote-{quoteId}.pdf", pdfAttachment) : null);

        _logger.LogInformation(
            "Quote approval email sent to {Email} for Quote #{QuoteId}, Workflow #{WorkflowId} (PDF attached: {HasPdf})",
            toEmail, quoteId, workflowId, pdfAttachment != null);
    }

    public async Task SendInvoiceGeneratedEmailAsync(
        string toEmail, string toName, int invoiceId, int workflowId, byte[]? pdfAttachment = null)
    {
        var adminEmail = _configuration["App:AdminEmail"] ?? "admin@invoicetracker.com";

        var htmlBody = EmailTemplates.GetInvoicePaymentTemplate(
            toName, invoiceId, workflowId, adminEmail);

        var subject = $"Invoice #{invoiceId} — Payment Required";

        await SendEmailAsync(toEmail, toName, subject, htmlBody,
            pdfAttachment != null ? ($"Invoice-{invoiceId}.pdf", pdfAttachment) : null);

        _logger.LogInformation(
            "Invoice payment email sent to {Email} for Invoice #{InvoiceId}, Workflow #{WorkflowId} (PDF attached: {HasPdf})",
            toEmail, invoiceId, workflowId, pdfAttachment != null);
    }

    public async Task SendOverdueInvoiceEmailAsync(
        string toEmail, string toName, int invoiceId, int workflowId,
        DateTime payByDate, byte[]? pdfAttachment = null)
    {
        var adminEmail = _configuration["App:AdminEmail"] ?? "admin@invoicetracker.com";
        var daysOverdue = (int)(DateTime.UtcNow - payByDate).TotalDays;

        var htmlBody = EmailTemplates.GetOverdueInvoiceTemplate(
            toName, invoiceId, workflowId, payByDate, daysOverdue, adminEmail);

        var subject = $"Invoice #{invoiceId} — Payment Overdue";

        await SendEmailAsync(toEmail, toName, subject, htmlBody,
            pdfAttachment != null ? ($"Invoice-{invoiceId}.pdf", pdfAttachment) : null);

        _logger.LogInformation(
            "Overdue reminder email sent to {Email} for Invoice #{InvoiceId}, Workflow #{WorkflowId} ({DaysOverdue} days overdue, PDF attached: {HasPdf})",
            toEmail, invoiceId, workflowId, daysOverdue, pdfAttachment != null);
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody,
        (string FileName, byte[] Data)? attachment = null)
    {
        var smtpHost = _configuration["Smtp:Host"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "1025");
        var useSsl = bool.Parse(_configuration["Smtp:UseSsl"] ?? "false");
        var username = _configuration["Smtp:Username"] ?? "";
        var password = _configuration["Smtp:Password"] ?? "";
        var fromAddress = _configuration["Smtp:FromAddress"] ?? "noreply@invoicetracker.local";
        var fromName = _configuration["Smtp:FromName"] ?? "Invoice Tracker";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        // Attach PDF if provided
        if (attachment.HasValue)
        {
            bodyBuilder.Attachments.Add(attachment.Value.FileName, attachment.Value.Data, new MimeKit.ContentType("application", "pdf"));
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            // MailHog does not require SSL or authentication
            await client.ConnectAsync(smtpHost, smtpPort, useSsl);

            if (!string.IsNullOrEmpty(username))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            throw;
        }
    }
}
