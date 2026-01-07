using PdfGeneratorService.Models;
using PuppeteerSharp;
using System.Text;

namespace PdfGeneratorService.Services.Generation;

public class PdfGenerationService : IPdfGenerationService
{
    private readonly ILogger<PdfGenerationService> _logger;
    private readonly string _templatePath;

    public PdfGenerationService(ILogger<PdfGenerationService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _templatePath = Path.Combine(env.ContentRootPath, "Templates", "InvoiceTemplate.html");
    }

    public async Task<byte[]> GeneratePdfFromInvoiceAsync(Invoice invoice)
    {
        try
        {
            _logger.LogInformation("Generating PDF for Invoice {InvoiceId}", invoice.Id);

            // Read the HTML template
            var htmlTemplate = await File.ReadAllTextAsync(_templatePath);

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
            .Replace("{ClientName}", invoice.ClientName)
            .Replace("{ClientSurname}", invoice.ClientSurname)
            .Replace("{ClientAddress}", invoice.ClientAddress)
            .Replace("{ClientCellphone}", invoice.ClientCellphone)
            .Replace("{InvoiceItems}", itemsHtml.ToString())
            .Replace("{Subtotal}", $"${subtotal:F2}")
            .Replace("{Total}", $"${total:F2}");
    }
}
