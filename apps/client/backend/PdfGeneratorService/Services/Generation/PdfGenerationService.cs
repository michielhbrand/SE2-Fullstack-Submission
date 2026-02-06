using PdfGeneratorService.Models;
using PdfGeneratorService.Services.Storage;
using PuppeteerSharp;
using System.Text;

namespace PdfGeneratorService.Services.Generation;

public class PdfGenerationService : IPdfGenerationService
{
    private readonly ILogger<PdfGenerationService> _logger;
    private readonly IMinioStorageService _storageService;
    private readonly string _templatePath;

    public PdfGenerationService(
        ILogger<PdfGenerationService> logger,
        IWebHostEnvironment env,
        IMinioStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
        _templatePath = Path.Combine(env.ContentRootPath, "Templates", "InvoiceTemplate.html");
    }

    public async Task<byte[]> GeneratePdfFromInvoiceAsync(Invoice invoice, string? templateName = null)
    {
        try
        {
            _logger.LogInformation("Generating PDF for Invoice {InvoiceId} with template {TemplateName}",
                invoice.Id, templateName ?? "default");

            // Read the HTML template
            string htmlTemplate;
            
            if (!string.IsNullOrEmpty(templateName))
            {
                // Fetch template from MinIO
                htmlTemplate = await _storageService.GetTemplateAsync(templateName);
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

    public async Task<byte[]> GeneratePdfFromQuoteAsync(Quote quote, string? templateName = null)
    {
        try
        {
            _logger.LogInformation("Generating PDF for Quote {QuoteId} with template {TemplateName}",
                quote.Id, templateName ?? "default");

            // Read the HTML template
            string htmlTemplate;
            
            if (!string.IsNullOrEmpty(templateName))
            {
                // Fetch template from MinIO
                htmlTemplate = await _storageService.GetQuoteTemplateAsync(templateName);
            }
            else
            {
                // Fall back to default quote template
                htmlTemplate = await _storageService.GetQuoteTemplateAsync("QuoteTemplate.html");
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
                    <td class='text-right'>{item.Amount}</td>
                    <td class='text-right'>${item.PricePerUnit:F2}</td>
                    <td class='text-right'>${item.TotalPrice:F2}</td>
                </tr>");
        }

        // Replace placeholders
        return template
            .Replace("{InvoiceId}", invoice.Id.ToString())
            .Replace("{DateCreated}", invoice.DateCreated.ToString("MMMM dd, yyyy"))
            .Replace("{ClientName}", invoice.Client?.Name ?? "")
            .Replace("{ClientSurname}", invoice.Client?.Surname ?? "")
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
                <td style='text-align: center;'>{item.Amount}</td>
                <td style='text-align: right;'>${item.PricePerUnit:F2}</td>
                <td style='text-align: right;'>${item.TotalPrice:F2}</td>
            </tr>");
        }

        // Replace Handlebars-style placeholders
        var result = template
            .Replace("{{QuoteId}}", quote.Id.ToString())
            .Replace("{{DateCreated}}", quote.DateCreated.ToString("MMMM dd, yyyy"))
            .Replace("{{ClientName}}", quote.Client?.Name ?? "")
            .Replace("{{ClientSurname}}", quote.Client?.Surname ?? "")
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
