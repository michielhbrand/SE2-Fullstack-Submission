using Shared.Database.Models;

namespace PdfGeneratorService.Services.Generation;

public interface IPdfGenerationService
{
    Task<byte[]> GeneratePdfFromInvoiceAsync(Invoice invoice, int? templateId = null);
    Task<byte[]> GeneratePdfFromQuoteAsync(Quote quote, int? templateId = null);
}
