using PdfGeneratorService.Models;

namespace PdfGeneratorService.Services.Generation;

public interface IPdfGenerationService
{
    Task<byte[]> GeneratePdfFromInvoiceAsync(Invoice invoice, string? templateName = null);
    Task<byte[]> GeneratePdfFromQuoteAsync(Quote quote, string? templateName = null);
}