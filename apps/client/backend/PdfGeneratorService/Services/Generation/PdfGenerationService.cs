using Shared.Database.Models;
using Shared.Database.Data;
using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.Services.Storage;
using PuppeteerSharp;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfGeneratorService.Services.Generation;

public class PdfGenerationService : IPdfGenerationService
{
    private readonly ILogger<PdfGenerationService> _logger;
    private readonly IMinioStorageService _storageService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _templatePath;
    private readonly string _quoteTemplatePath;

    public PdfGenerationService(
        ILogger<PdfGenerationService> logger,
        IWebHostEnvironment env,
        IMinioStorageService storageService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _storageService = storageService;
        _serviceProvider = serviceProvider;
        _templatePath = Path.Combine(env.ContentRootPath, "Templates", "InvoiceTemplate.html");
        _quoteTemplatePath = Path.Combine(env.ContentRootPath, "Templates", "QuoteTemplate.html");
    }

    public async Task<byte[]> GeneratePdfFromInvoiceAsync(Invoice invoice, List<BankAccount> bankAccounts, int? templateId = null)
    {
        try
        {
            _logger.LogInformation("Generating PDF for Invoice {InvoiceId} with templateId {TemplateId}",
                invoice.Id, templateId?.ToString() ?? "default");

            string htmlTemplate;

            if (templateId.HasValue)
            {
                htmlTemplate = await GetTemplateHtmlByIdAsync(templateId.Value, TemplateType.Invoice);
            }
            else
            {
                try
                {
                    htmlTemplate = await _storageService.GetTemplateAsync("InvoiceTemplate.html");
                }
                catch
                {
                    htmlTemplate = await File.ReadAllTextAsync(_templatePath);
                }
            }

            var htmlContent = PopulateInvoiceTemplate(htmlTemplate, invoice, bankAccounts);

            return await RenderPdfAsync(htmlContent, $"Invoice {invoice.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for Invoice {InvoiceId}", invoice.Id);
            throw;
        }
    }

    public async Task<byte[]> GeneratePdfFromQuoteAsync(Quote quote, int? templateId = null)
    {
        try
        {
            _logger.LogInformation("Generating PDF for Quote {QuoteId} with templateId {TemplateId}",
                quote.Id, templateId?.ToString() ?? "default");

            string htmlTemplate;

            if (templateId.HasValue)
            {
                htmlTemplate = await GetTemplateHtmlByIdAsync(templateId.Value, TemplateType.Quote);
            }
            else
            {
                try
                {
                    htmlTemplate = await _storageService.GetQuoteTemplateAsync("QuoteTemplate.html");
                }
                catch
                {
                    htmlTemplate = await File.ReadAllTextAsync(_quoteTemplatePath);
                }
            }

            var htmlContent = PopulateQuoteTemplate(htmlTemplate, quote);

            return await RenderPdfAsync(htmlContent, $"Quote {quote.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for Quote {QuoteId}", quote.Id);
            throw;
        }
    }

    /// <summary>
    /// Renders HTML content to a PDF byte array using Puppeteer.
    /// </summary>
    private async Task<byte[]> RenderPdfAsync(string htmlContent, string documentLabel)
    {
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        });

        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(htmlContent);

        var pdfBytes = await page.PdfDataAsync(new PdfOptions
        {
            Format = PuppeteerSharp.Media.PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new PuppeteerSharp.Media.MarginOptions
            {
                Top = "20px",
                Right = "20px",
                Bottom = "20px",
                Left = "20px"
            }
        });

        _logger.LogInformation("PDF generated successfully for {Label}, Size: {Size} bytes",
            documentLabel, pdfBytes.Length);

        return pdfBytes;
    }

    /// <summary>
    /// Looks up a template by ID from the database, then fetches the HTML content from MinIO using the StorageKey.
    /// </summary>
    private async Task<string> GetTemplateHtmlByIdAsync(int templateId, TemplateType expectedType)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var template = await dbContext.Templates.FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            _logger.LogWarning("Template {TemplateId} not found in database, falling back to default", templateId);
            throw new InvalidOperationException($"Template with ID {templateId} not found");
        }

        // Safety net: reject template type mismatches (e.g. quote template used for invoice)
        if (template.Type != expectedType)
        {
            _logger.LogWarning(
                "Template {TemplateId} is type {ActualType} but expected {ExpectedType}, falling back to default",
                templateId, template.Type, expectedType);
            throw new InvalidOperationException(
                $"Template {templateId} is type {template.Type} but expected {expectedType}");
        }

        _logger.LogInformation("Found template {TemplateId}: {TemplateName} (Type: {Type}, StorageKey: {StorageKey})",
            template.Id, template.Name, template.Type, template.StorageKey);

        var parts = template.StorageKey.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid StorageKey format for template {templateId}: {template.StorageKey}");
        }

        if (template.Type == TemplateType.Quote)
        {
            return await _storageService.GetQuoteTemplateAsync(parts[1]);
        }
        else
        {
            return await _storageService.GetTemplateAsync(parts[1]);
        }
    }

    // ─── Invoice Template Population ──────────────────────────────────────────

    private string PopulateInvoiceTemplate(string template, Invoice invoice, List<BankAccount> bankAccounts)
    {
        var result = template;

        // Group 1: Title
        result = result.Replace("{{DocumentTitle}}", "INVOICE");

        // Group 3: Organization & document meta
        result = PopulateOrganizationPlaceholders(result, invoice.Organization);
        result = result.Replace("{{DocumentNumber}}", $"INV-{invoice.Id}");
        result = result.Replace("{{DateCreated}}", invoice.DateCreated.ToString("MMMM dd, yyyy"));
        result = result.Replace("{{PayByDate}}", invoice.PayByDate.ToString("MMMM dd, yyyy"));

        // Group 2: Client details
        result = PopulateClientPlaceholders(result, invoice.Client);

        // Group 4: Items & VAT
        var itemsHtml = BuildInvoiceItemsHtml(invoice.Items);
        result = result.Replace("{{InvoiceItems}}", itemsHtml);
        var orgVatRate = invoice.Organization?.VatRate ?? 15m;
        result = PopulateVatSection(result, invoice.Items.Sum(i => i.TotalPrice), invoice.VatInclusive, orgVatRate);

        // Group 5: Bank details
        result = PopulateBankDetails(result, bankAccounts);
        result = PopulateConditionalBlock(result, "PROOF_OF_PAYMENT", "{{ProofOfPaymentEmail}}", invoice.Organization?.Email);
        result = result.Replace("{{ProofOfPaymentEmail}}", invoice.Organization?.Email ?? "");

        return result;
    }

    // ─── Quote Template Population ────────────────────────────────────────────

    private string PopulateQuoteTemplate(string template, Quote quote)
    {
        var result = template;

        // Group 1: Title
        result = result.Replace("{{DocumentTitle}}", "QUOTATION");

        // Group 3: Organization & document meta
        result = PopulateOrganizationPlaceholders(result, quote.Organization);
        result = result.Replace("{{DocumentNumber}}", $"Q-{quote.Id}");
        result = result.Replace("{{DateCreated}}", quote.DateCreated.ToString("MMMM dd, yyyy"));

        // Group 2: Client details
        result = PopulateClientPlaceholders(result, quote.Client);

        // Group 4: Items & VAT
        var itemsHtml = BuildQuoteItemsHtml(quote.Items);
        result = result.Replace("{{QuoteItems}}", itemsHtml);
        var orgVatRate = quote.Organization?.VatRate ?? 15m;
        result = PopulateVatSection(result, quote.Items.Sum(i => i.TotalPrice), quote.VatInclusive, orgVatRate);

        // Group 5: Quote footer uses {{OrganizationPhone}} which is already replaced above

        return result;
    }

    // ─── Shared Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Populates organization-related placeholders and conditionally removes blocks if data is missing.
    /// </summary>
    private string PopulateOrganizationPlaceholders(string template, Organization? org)
    {
        var result = template;

        result = result.Replace("{{OrganizationName}}", org?.Name ?? "");

        // Build formatted address
        string? formattedAddress = null;
        if (org?.Address != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(org.Address.Street)) parts.Add(org.Address.Street);
            if (!string.IsNullOrWhiteSpace(org.Address.City)) parts.Add(org.Address.City);
            if (!string.IsNullOrWhiteSpace(org.Address.State)) parts.Add(org.Address.State);
            if (!string.IsNullOrWhiteSpace(org.Address.PostalCode)) parts.Add(org.Address.PostalCode);
            if (!string.IsNullOrWhiteSpace(org.Address.Country)) parts.Add(org.Address.Country);
            if (parts.Count > 0) formattedAddress = string.Join(", ", parts);
        }

        result = PopulateConditionalBlock(result, "ORG_ADDRESS", "{{OrganizationAddress}}", formattedAddress);
        result = result.Replace("{{OrganizationAddress}}", formattedAddress ?? "");

        result = PopulateConditionalBlock(result, "ORG_PHONE", "{{OrganizationPhone}}", org?.Phone);
        result = result.Replace("{{OrganizationPhone}}", org?.Phone ?? "");

        result = PopulateConditionalBlock(result, "ORG_EMAIL", "{{OrganizationEmail}}", org?.Email);
        result = result.Replace("{{OrganizationEmail}}", org?.Email ?? "");

        return result;
    }

    /// <summary>
    /// Populates client-related placeholders and conditionally removes blocks if data is missing.
    /// </summary>
    private string PopulateClientPlaceholders(string template, Client? client)
    {
        var result = template;

        result = result.Replace("{{ClientName}}", client?.Name ?? "");

        result = PopulateConditionalBlock(result, "CLIENT_ADDRESS", "{{ClientAddress}}", client?.Address);
        result = result.Replace("{{ClientAddress}}", client?.Address ?? "");

        result = PopulateConditionalBlock(result, "CLIENT_TELEPHONE", "{{ClientTelephone}}", client?.Cellphone);
        result = result.Replace("{{ClientTelephone}}", client?.Cellphone ?? "");

        result = PopulateConditionalBlock(result, "CLIENT_EMAIL", "{{ClientEmail}}", client?.Email);
        result = result.Replace("{{ClientEmail}}", client?.Email ?? "");

        result = PopulateConditionalBlock(result, "CLIENT_VAT", "{{ClientVatNumber}}", client?.VatNumber);
        result = result.Replace("{{ClientVatNumber}}", client?.VatNumber ?? "");

        return result;
    }

    /// <summary>
    /// Handles VAT section: if VatInclusive, removes the VAT breakdown block; otherwise calculates and shows it.
    /// </summary>
    /// <param name="template">HTML template string</param>
    /// <param name="itemsSubtotal">Sum of line item totals</param>
    /// <param name="vatInclusive">Whether prices include VAT</param>
    /// <param name="vatRatePercent">Organization's VAT rate as a percentage (e.g. 15 means 15%)</param>
    private string PopulateVatSection(string template, decimal itemsSubtotal, bool vatInclusive, decimal vatRatePercent)
    {
        var result = template;
        var vatRateDecimal = vatRatePercent / 100m;

        if (vatInclusive)
        {
            // Prices include VAT — show total only, remove VAT breakdown
            result = RemoveConditionalBlock(result, "VAT_SECTION");
            result = result.Replace("{{Subtotal}}", $"R {itemsSubtotal:N2}");
            result = result.Replace("{{Total}}", $"R {itemsSubtotal:N2}");
            result = result.Replace("{{VatNote}}", "All prices include VAT");
        }
        else
        {
            // Prices exclude VAT — show subtotal, VAT amount, and total
            var vatAmount = itemsSubtotal * vatRateDecimal;
            var total = itemsSubtotal + vatAmount;

            result = result.Replace("{{Subtotal}}", $"R {itemsSubtotal:N2}");
            result = result.Replace("{{VatAmount}}", $"R {vatAmount:N2}");
            result = result.Replace("{{Total}}", $"R {total:N2}");
            result = result.Replace("{{VatNote}}", $"VAT calculated at {vatRatePercent:G}%");
        }

        return result;
    }

    /// <summary>
    /// Populates bank details section for invoices. Removes the block if no active bank accounts exist.
    /// </summary>
    private string PopulateBankDetails(string template, List<BankAccount> bankAccounts)
    {
        var result = template;
        var activeBankAccount = bankAccounts.FirstOrDefault(b => b.Active);

        if (activeBankAccount == null)
        {
            result = RemoveConditionalBlock(result, "BANK_DETAILS");
        }
        else
        {
            result = result.Replace("{{BankName}}", activeBankAccount.BankName);
            result = result.Replace("{{BranchCode}}", activeBankAccount.BranchCode);
            result = result.Replace("{{AccountNumber}}", activeBankAccount.AccountNumber);
            result = result.Replace("{{AccountType}}", activeBankAccount.AccountType);
        }

        return result;
    }

    /// <summary>
    /// If the value is null or empty, removes the entire HTML block between the START and END comment markers.
    /// Otherwise leaves the block intact.
    /// </summary>
    private string PopulateConditionalBlock(string template, string blockName, string placeholder, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RemoveConditionalBlock(template, blockName);
        }
        return template;
    }

    /// <summary>
    /// Removes an HTML block delimited by comment markers: &lt;!-- BLOCKNAME_START --&gt; ... &lt;!-- BLOCKNAME_END --&gt;
    /// </summary>
    private string RemoveConditionalBlock(string template, string blockName)
    {
        var pattern = $@"<!--\s*{Regex.Escape(blockName)}_START\s*-->.*?<!--\s*{Regex.Escape(blockName)}_END\s*-->";
        return Regex.Replace(template, pattern, "", RegexOptions.Singleline);
    }

    // ─── Item Row Builders ────────────────────────────────────────────────────

    private string BuildInvoiceItemsHtml(ICollection<InvoiceItem> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            sb.AppendLine($@"
                <tr>
                    <td>{item.Description}</td>
                    <td class=""text-center"">{item.Quantity}</td>
                    <td class=""text-right"">R {item.PricePerUnit:N2}</td>
                    <td class=""text-right"">R {item.TotalPrice:N2}</td>
                </tr>");
        }
        return sb.ToString();
    }

    private string BuildQuoteItemsHtml(ICollection<QuoteItem> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            sb.AppendLine($@"
                <tr>
                    <td>{item.Description}</td>
                    <td class=""text-center"">{item.Quantity}</td>
                    <td class=""text-right"">R {item.PricePerUnit:N2}</td>
                    <td class=""text-right"">R {item.TotalPrice:N2}</td>
                </tr>");
        }
        return sb.ToString();
    }
}
