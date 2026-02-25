using Shared.Database.Models;
using Shared.Database.Data;
using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.Services.Storage;
using PuppeteerSharp;
using System.Text;

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

    public async Task<byte[]> GeneratePdfFromInvoiceAsync(Invoice invoice, int? templateId = null)
    {
        try
        {
            _logger.LogInformation("Generating PDF for Invoice {InvoiceId} with templateId {TemplateId}",
                invoice.Id, templateId?.ToString() ?? "default");

            // Read the HTML template
            string htmlTemplate;
            
            if (templateId.HasValue)
            {
                // Look up template from DB and fetch from MinIO by StorageKey
                htmlTemplate = await GetTemplateHtmlByIdAsync(templateId.Value, TemplateType.Invoice);
            }
            else
            {
                // Fall back to local template file
                htmlTemplate = await File.ReadAllTextAsync(_templatePath);
            }

            // Replace placeholders with actual data
            var htmlContent = PopulateTemplate(htmlTemplate, invoice);

            // Ensure browser is downloaded
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            // Generate PDF using Puppeteer
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

            _logger.LogInformation("PDF generated successfully for Invoice {InvoiceId}, Size: {Size} bytes", 
                invoice.Id, pdfBytes.Length);

            return pdfBytes;
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

            // Read the HTML template
            string htmlTemplate;
            
            if (templateId.HasValue)
            {
                // Look up template from DB and fetch from MinIO by StorageKey
                htmlTemplate = await GetTemplateHtmlByIdAsync(templateId.Value, TemplateType.Quote);
            }
            else
            {
                // Fall back to default quote template from MinIO or local file
                try
                {
                    htmlTemplate = await _storageService.GetQuoteTemplateAsync("QuoteTemplate.html");
                }
                catch
                {
                    htmlTemplate = await File.ReadAllTextAsync(_quoteTemplatePath);
                }
            }

            // Replace placeholders with actual data
            var htmlContent = PopulateQuoteTemplate(htmlTemplate, quote);

            // Ensure browser is downloaded
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            // Generate PDF using Puppeteer
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

            _logger.LogInformation("PDF generated successfully for Quote {QuoteId}, Size: {Size} bytes",
                quote.Id, pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for Quote {QuoteId}", quote.Id);
            throw;
        }
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

        _logger.LogInformation("Found template {TemplateId}: {TemplateName} (Type: {Type}, StorageKey: {StorageKey})",
            template.Id, template.Name, template.Type, template.StorageKey);

        // Parse the StorageKey (format: "bucket/objectName")
        var parts = template.StorageKey.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid StorageKey format for template {templateId}: {template.StorageKey}");
        }

        var bucketName = parts[0];
        var objectName = parts[1];

        // Fetch from the appropriate bucket based on the StorageKey
        if (template.Type == TemplateType.Quote)
        {
            return await _storageService.GetQuoteTemplateAsync(objectName);
        }
        else
        {
            return await _storageService.GetTemplateAsync(objectName);
        }
    }

    private string PopulateTemplate(string template, Invoice invoice)
    {
        var subtotal = invoice.Items.Sum(item => item.TotalPrice);
        var total = subtotal; // Add tax calculation if needed

        // Build invoice items HTML
        var itemsHtml = new StringBuilder();
        foreach (var item in invoice.Items)
        {
            itemsHtml.AppendLine($@"
                <tr>
                    <td>{item.Description}</td>
                    <td class='text-right'>{item.Quantity}</td>
                    <td class='text-right'>${item.PricePerUnit:F2}</td>
                    <td class='text-right'>${item.TotalPrice:F2}</td>
                </tr>");
        }

        // Replace placeholders
        return template
            .Replace("{InvoiceId}", invoice.Id.ToString())
            .Replace("{DateCreated}", invoice.DateCreated.ToString("MMMM dd, yyyy"))
            .Replace("{PayByDate}", invoice.PayByDate.ToString("MMMM dd, yyyy"))
            .Replace("{ClientName}", invoice.Client?.Name ?? "")
            .Replace("{ClientSurname}", "")
            .Replace("{ClientAddress}", invoice.Client?.Address ?? "")
            .Replace("{ClientCellphone}", invoice.Client?.Cellphone ?? "")
            .Replace("{InvoiceItems}", itemsHtml.ToString())
            .Replace("{Subtotal}", $"${subtotal:F2}")
            .Replace("{Total}", $"${total:F2}");
    }

    private string PopulateQuoteTemplate(string template, Quote quote)
    {
        var total = quote.Items.Sum(item => item.TotalPrice);

        // Build quote items HTML
        var itemsHtml = new StringBuilder();
        foreach (var item in quote.Items)
        {
            itemsHtml.AppendLine($@"
            <tr>
                <td>{item.Description}</td>
                <td style='text-align: center;'>{item.Quantity}</td>
                <td style='text-align: right;'>${item.PricePerUnit:F2}</td>
                <td style='text-align: right;'>${item.TotalPrice:F2}</td>
            </tr>");
        }

        // Replace Handlebars-style placeholders
        var result = template
            .Replace("{{QuoteId}}", quote.Id.ToString())
            .Replace("{{DateCreated}}", quote.DateCreated.ToString("MMMM dd, yyyy"))
            .Replace("{{ClientName}}", quote.Client?.Name ?? "")
            .Replace("{{ClientSurname}}", "")
            .Replace("{{ClientAddress}}", quote.Client?.Address ?? "")
            .Replace("{{ClientCellphone}}", quote.Client?.Cellphone ?? "")
            .Replace("{{Total}}", $"{total:F2}");

        // Replace the items section (between {{#each Items}} and {{/each}})
        var itemsStartMarker = "{{#each Items}}";
        var itemsEndMarker = "{{/each}}";
        var startIndex = result.IndexOf(itemsStartMarker);
        var endIndex = result.IndexOf(itemsEndMarker);
        
        if (startIndex >= 0 && endIndex >= 0)
        {
            var length = endIndex + itemsEndMarker.Length - startIndex;
            result = result.Remove(startIndex, length).Insert(startIndex, itemsHtml.ToString());
        }

        return result;
    }
}
