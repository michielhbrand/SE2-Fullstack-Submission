using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PdfGeneratorService.Services.Generation;
using PdfGeneratorService.Services.Storage;
using Shared.Database.Models;

namespace PdfGeneratorService.Tests.UnitTests;

/// <summary>
/// Unit tests for the pure template-population helpers in PdfGenerationService.
/// These methods are made `internal` so the test project can access them via InternalsVisibleTo.
/// Puppeteer and MinIO are NOT involved — only in-memory string manipulation is tested.
/// </summary>
public class PdfGenerationServiceTests
{
    private readonly PdfGenerationService _service;

    public PdfGenerationServiceTests()
    {
        var loggerMock = new Mock<ILogger<PdfGenerationService>>();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        var storageMock = new Mock<IMinioStorageService>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        _service = new PdfGenerationService(
            loggerMock.Object,
            envMock.Object,
            storageMock.Object,
            serviceProviderMock.Object);
    }

    // ─── VAT Section ──────────────────────────────────────────────────────────

    [Fact]
    public void PopulateVatSection_VatInclusive_ShowsTotalOnly_RemovesVatBlock()
    {
        var template = "<!-- VAT_SECTION_START -->VAT breakdown{{VatAmount}}<!-- VAT_SECTION_END -->{{Subtotal}}{{Total}}{{VatNote}}";

        var result = _service.PopulateVatSection(template, 1000m, vatInclusive: true, vatRatePercent: 15m);

        result.Should().NotContain("VAT_SECTION_START");
        result.Should().NotContain("{{VatAmount}}");
        result.Should().NotContain("{{Subtotal}}");
        result.Should().NotContain("{{Total}}");
        result.Should().Contain("All prices include VAT");
    }

    [Fact]
    public void PopulateVatSection_VatExclusive_CalculatesVatAmount()
    {
        var template = "{{Subtotal}}{{VatAmount}}{{Total}}{{VatNote}}<!-- VAT_SECTION_START -->x<!-- VAT_SECTION_END -->";

        var result = _service.PopulateVatSection(template, 1000m, vatInclusive: false, vatRatePercent: 15m);

        result.Should().NotContain("{{Subtotal}}");
        result.Should().NotContain("{{VatAmount}}");
        result.Should().NotContain("{{Total}}");
        result.Should().Contain("VAT calculated at 15%");
    }

    [Fact]
    public void PopulateVatSection_VatExclusive_ZeroRate_AddNoVat()
    {
        var template = "{{Subtotal}}{{VatAmount}}{{Total}}{{VatNote}}";

        var result = _service.PopulateVatSection(template, 500m, vatInclusive: false, vatRatePercent: 0m);

        result.Should().NotContain("{{Subtotal}}");
        result.Should().NotContain("{{VatAmount}}");
        result.Should().Contain("VAT calculated at 0%");
    }

    // ─── Bank Details ─────────────────────────────────────────────────────────

    [Fact]
    public void PopulateBankDetails_ActiveAccountExists_PopulatesPlaceholders()
    {
        var template = "{{BankName}}{{BranchCode}}{{AccountNumber}}{{AccountType}}<!-- BANK_DETAILS_START -->x<!-- BANK_DETAILS_END -->";
        var accounts = new List<BankAccount>
        {
            new() { BankName = "FNB", BranchCode = "250655", AccountNumber = "62123456789", AccountType = "Cheque", Active = true, OrganizationId = 1 }
        };

        var result = _service.PopulateBankDetails(template, accounts);

        result.Should().Contain("FNB");
        result.Should().Contain("250655");
        result.Should().Contain("62123456789");
        result.Should().Contain("Cheque");
    }

    [Fact]
    public void PopulateBankDetails_NoActiveAccount_RemovesBankDetailsBlock()
    {
        var template = "<!-- BANK_DETAILS_START -->BankName: {{BankName}}<!-- BANK_DETAILS_END -->Other content";
        var accounts = new List<BankAccount>
        {
            new() { BankName = "FNB", BranchCode = "250655", AccountNumber = "123", AccountType = "Cheque", Active = false, OrganizationId = 1 }
        };

        var result = _service.PopulateBankDetails(template, accounts);

        result.Should().NotContain("{{BankName}}");
        result.Should().NotContain("BANK_DETAILS_START");
        result.Should().Contain("Other content");
    }

    [Fact]
    public void PopulateBankDetails_EmptyList_RemovesBankDetailsBlock()
    {
        var template = "<!-- BANK_DETAILS_START -->bank info<!-- BANK_DETAILS_END -->";

        var result = _service.PopulateBankDetails(template, new List<BankAccount>());

        result.Should().NotContain("bank info");
        result.Trim().Should().BeEmpty();
    }

    // ─── Conditional Block Removal ────────────────────────────────────────────

    [Fact]
    public void RemoveConditionalBlock_RemovesBlockBetweenMarkers()
    {
        var template = "Before<!-- SECTION_START -->Sensitive content<!-- SECTION_END -->After";

        var result = _service.RemoveConditionalBlock(template, "SECTION");

        result.Should().Be("BeforeAfter");
    }

    [Fact]
    public void RemoveConditionalBlock_MultilineBlock_IsRemoved()
    {
        var template = "Before\n<!-- SECTION_START -->\nline1\nline2\n<!-- SECTION_END -->\nAfter";

        var result = _service.RemoveConditionalBlock(template, "SECTION");

        result.Should().Contain("Before");
        result.Should().Contain("After");
        result.Should().NotContain("line1");
        result.Should().NotContain("line2");
    }

    [Fact]
    public void RemoveConditionalBlock_NoMatchingBlock_ReturnsOriginal()
    {
        var template = "No markers here";

        var result = _service.RemoveConditionalBlock(template, "NONEXISTENT");

        result.Should().Be("No markers here");
    }

    // ─── Item Row Builders ────────────────────────────────────────────────────

    [Fact]
    public void BuildInvoiceItemsHtml_GeneratesTableRowPerItem()
    {
        var items = new List<InvoiceItem>
        {
            new() { Description = "Consulting", Quantity = 2, PricePerUnit = 500m },
            new() { Description = "Support",    Quantity = 1, PricePerUnit = 250m }
        };

        var html = _service.BuildInvoiceItemsHtml(items);

        html.Should().Contain("Consulting");
        html.Should().Contain("Support");
        html.Should().Contain("<tr>", "each item should produce a table row");
        // Two items → two <tr> blocks
        html.Split("<tr>").Length.Should().BeGreaterThan(2);
    }

    [Fact]
    public void BuildQuoteItemsHtml_GeneratesTableRowPerItem()
    {
        var items = new List<QuoteItem>
        {
            new() { Description = "Website design", Quantity = 1, PricePerUnit = 8000m }
        };

        var html = _service.BuildQuoteItemsHtml(items);

        html.Should().Contain("Website design");
        html.Should().Contain("<tr>");
    }
}
