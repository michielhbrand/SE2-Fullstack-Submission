namespace EmailNotificationService.Services;

/// <summary>
/// Static HTML email templates for workflow notifications
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Quote approval email with Accept/Reject buttons
    /// </summary>
    public static string GetQuoteApprovalTemplate(
        string clientName, int quoteId, int workflowId,
        string approveUrl, string rejectUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Quote Approval Required</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f4f4f5;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f5; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #4f46e5; padding: 32px 40px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 24px; font-weight: 600;"">
                                Quote Approval Required
                            </h1>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""margin: 0 0 16px; color: #374151; font-size: 16px; line-height: 1.6;"">
                                Hello <strong>{clientName}</strong>,
                            </p>
                            <p style=""margin: 0 0 24px; color: #374151; font-size: 16px; line-height: 1.6;"">
                                A quote has been prepared for your review. Please review the details and approve or reject the quote using the buttons below.
                            </p>

                            <!-- Quote Info Card -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; margin-bottom: 32px;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""color: #6b7280; font-size: 14px; padding-bottom: 8px;"">Quote Number</td>
                                                <td style=""color: #111827; font-size: 14px; font-weight: 600; text-align: right; padding-bottom: 8px;"">#{quoteId}</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #6b7280; font-size: 14px;"">Workflow Reference</td>
                                                <td style=""color: #111827; font-size: 14px; font-weight: 600; text-align: right;"">#{workflowId}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Buttons -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding-bottom: 16px;"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""padding-right: 12px;"">
                                                    <a href=""{approveUrl}""
                                                       style=""display: inline-block; background-color: #059669; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 6px; text-align: center;"">
                                                        ✓ Approve Quote
                                                    </a>
                                                </td>
                                                <td style=""padding-left: 12px;"">
                                                    <a href=""{rejectUrl}""
                                                       style=""display: inline-block; background-color: #dc2626; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 6px; text-align: center;"">
                                                        ✗ Reject Quote
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 24px 0 0; color: #9ca3af; font-size: 13px; line-height: 1.5; text-align: center;"">
                                This link will expire in 7 days. If you have questions, please contact us directly.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 24px 40px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0; color: #9ca3af; font-size: 12px; text-align: center;"">
                                This is an automated message from Invoice Tracker. Please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Invoice payment request email — asks client to settle and email proof of payment
    /// </summary>
    public static string GetInvoicePaymentTemplate(
        string clientName, int invoiceId, int workflowId, string adminEmail)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invoice — Payment Required</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f4f4f5;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f5; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #7c3aed; padding: 32px 40px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 24px; font-weight: 600;"">
                                Payment Required
                            </h1>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""margin: 0 0 16px; color: #374151; font-size: 16px; line-height: 1.6;"">
                                Hello <strong>{clientName}</strong>,
                            </p>
                            <p style=""margin: 0 0 16px; color: #374151; font-size: 16px; line-height: 1.6;"">
                                Please find attached your invoice for settlement. Kindly review the details below and arrange payment at your earliest convenience.
                            </p>

                            <!-- Invoice Info Card -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; margin-bottom: 24px;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""color: #6b7280; font-size: 14px; padding-bottom: 8px;"">Invoice Number</td>
                                                <td style=""color: #111827; font-size: 14px; font-weight: 600; text-align: right; padding-bottom: 8px;"">#{invoiceId}</td>
                                            </tr>
                                            <tr>
                                                <td style=""color: #6b7280; font-size: 14px;"">Reference</td>
                                                <td style=""color: #111827; font-size: 14px; font-weight: 600; text-align: right;"">WF-{workflowId}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Instructions -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #faf5ff; border: 1px solid #e9d5ff; border-radius: 8px; margin-bottom: 24px;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 8px; color: #6b21a8; font-size: 14px; font-weight: 600;"">
                                            How to complete payment:
                                        </p>
                                        <ol style=""margin: 0; padding-left: 20px; color: #374151; font-size: 14px; line-height: 1.8;"">
                                            <li>Review the attached invoice PDF for the amount due and banking details.</li>
                                            <li>Make payment using your preferred method (EFT / bank transfer).</li>
                                            <li>Email your proof of payment to <a href=""mailto:{adminEmail}"" style=""color: #7c3aed; font-weight: 600;"">{adminEmail}</a>, quoting invoice <strong>#{invoiceId}</strong>.</li>
                                        </ol>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0; color: #6b7280; font-size: 14px; line-height: 1.6;"">
                                If you have any questions regarding this invoice, please contact us at
                                <a href=""mailto:{adminEmail}"" style=""color: #7c3aed;"">{adminEmail}</a>.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 24px 40px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0; color: #9ca3af; font-size: 12px; text-align: center;"">
                                This is an automated message from Invoice Tracker. Please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Quote response confirmation page (shown after clicking Accept/Reject)
    /// </summary>
    public static string GetQuoteResponseConfirmationHtml(string action, int quoteId, bool success, string? errorMessage = null)
    {
        var isApprove = action.Equals("approve", StringComparison.OrdinalIgnoreCase);
        var title = success
            ? (isApprove ? "Quote Approved" : "Quote Rejected")
            : "Action Failed";
        var color = success
            ? (isApprove ? "#059669" : "#dc2626")
            : "#6b7280";
        var icon = success
            ? (isApprove ? "✓" : "✗")
            : "⚠";
        var message = success
            ? (isApprove
                ? $"Quote #{quoteId} has been approved successfully. The workflow will proceed to invoice generation."
                : $"Quote #{quoteId} has been rejected. The sender will be notified and may modify and resend the quote.")
            : (errorMessage ?? "An error occurred while processing your response.");

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f4f4f5; display: flex; justify-content: center; align-items: center; min-height: 100vh;"">
    <div style=""max-width: 500px; margin: 80px auto; background: #ffffff; border-radius: 12px; padding: 48px; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
        <div style=""font-size: 48px; margin-bottom: 16px;"">{icon}</div>
        <h1 style=""margin: 0 0 16px; color: {color}; font-size: 28px; font-weight: 700;"">{title}</h1>
        <p style=""margin: 0; color: #6b7280; font-size: 16px; line-height: 1.6;"">{message}</p>
        <p style=""margin: 32px 0 0; color: #9ca3af; font-size: 13px;"">You can close this window.</p>
    </div>
</body>
</html>";
    }
}
