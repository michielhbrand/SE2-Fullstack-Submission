using FluentAssertions;
using EmailNotificationService.Services;

namespace EmailNotificationService.Tests.UnitTests;

/// <summary>
/// Unit tests for EmailTemplates — pure static HTML generation, no I/O required.
/// </summary>
public class EmailTemplatesTests
{
    // ─── Quote Approval Template ──────────────────────────────────────────────

    [Fact]
    public void GetQuoteApprovalTemplate_ContainsClientName()
    {
        var html = EmailTemplates.GetQuoteApprovalTemplate(
            clientName: "Jane Smith",
            quoteId: 42, workflowId: 7,
            approveUrl: "https://example.com/approve",
            rejectUrl: "https://example.com/reject");

        html.Should().Contain("Jane Smith");
    }

    [Fact]
    public void GetQuoteApprovalTemplate_ContainsQuoteAndWorkflowIds()
    {
        var html = EmailTemplates.GetQuoteApprovalTemplate("Client", 99, 5,
            "https://example.com/approve", "https://example.com/reject");

        html.Should().Contain("#99");  // quote number
        html.Should().Contain("#5");   // workflow reference
    }

    [Fact]
    public void GetQuoteApprovalTemplate_ContainsApproveAndRejectUrls()
    {
        var html = EmailTemplates.GetQuoteApprovalTemplate("Client", 1, 1,
            approveUrl: "https://example.com/approve?token=abc",
            rejectUrl:  "https://example.com/reject?token=xyz");

        html.Should().Contain("https://example.com/approve?token=abc");
        html.Should().Contain("https://example.com/reject?token=xyz");
    }

    [Fact]
    public void GetQuoteApprovalTemplate_ContainsExpiryNotice()
    {
        var html = EmailTemplates.GetQuoteApprovalTemplate("Client", 1, 1, "a", "b");

        html.Should().Contain("7 days", "links expire after 7 days per spec");
    }

    [Fact]
    public void GetQuoteApprovalTemplate_IsValidHtml()
    {
        var html = EmailTemplates.GetQuoteApprovalTemplate("Client", 1, 1, "a", "b");

        html.Should().StartWith("\n<!DOCTYPE html>");
        html.Should().Contain("</html>");
    }

    // ─── Invoice Payment Template ─────────────────────────────────────────────

    [Fact]
    public void GetInvoicePaymentTemplate_ContainsClientNameAndInvoiceId()
    {
        var html = EmailTemplates.GetInvoicePaymentTemplate(
            clientName: "Acme Corp",
            invoiceId: 123, workflowId: 9,
            adminEmail: "admin@company.com");

        html.Should().Contain("Acme Corp");
        html.Should().Contain("#123");
        html.Should().Contain("WF-9");
    }

    [Fact]
    public void GetInvoicePaymentTemplate_ContainsAdminEmailForProofOfPayment()
    {
        var html = EmailTemplates.GetInvoicePaymentTemplate("Client", 1, 1, "finance@company.com");

        html.Should().Contain("finance@company.com");
    }

    // ─── Quote Response Confirmation ──────────────────────────────────────────

    [Theory]
    [InlineData("approve", true, "Quote Approved")]
    [InlineData("reject",  true, "Quote Rejected")]
    [InlineData("approve", false, "Action Failed")]
    [InlineData("reject",  false, "Action Failed")]
    public void GetQuoteResponseConfirmationHtml_TitleMatchesActionAndSuccess(
        string action, bool success, string expectedTitle)
    {
        var html = EmailTemplates.GetQuoteResponseConfirmationHtml(action, quoteId: 1, success);

        html.Should().Contain(expectedTitle);
    }

    [Fact]
    public void GetQuoteResponseConfirmationHtml_Approve_Success_ContainsQuoteId()
    {
        var html = EmailTemplates.GetQuoteResponseConfirmationHtml("approve", quoteId: 55, success: true);

        html.Should().Contain("55");
    }

    [Fact]
    public void GetQuoteResponseConfirmationHtml_Failure_ContainsErrorMessage()
    {
        var html = EmailTemplates.GetQuoteResponseConfirmationHtml(
            "approve", quoteId: 1, success: false, errorMessage: "Token has expired");

        html.Should().Contain("Token has expired");
    }
}
