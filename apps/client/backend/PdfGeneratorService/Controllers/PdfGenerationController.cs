using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.Services.Generation;
using PdfGeneratorService.Services.Storage;
using Shared.Database.Data;

namespace PdfGeneratorService.Controllers;

/// <summary>
/// Generates PDFs for existing invoices and quotes on demand.
/// Used by the management portal seed endpoint to produce PDFs for demo data
/// without triggering Kafka events or email notifications.
/// </summary>
[ApiController]
[Route("api/pdf")]
[Produces("application/json")]
public class PdfGenerationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPdfGenerationService _pdfService;
    private readonly IMinioStorageService _minioService;
    private readonly ILogger<PdfGenerationController> _logger;

    public PdfGenerationController(
        ApplicationDbContext db,
        IPdfGenerationService pdfService,
        IMinioStorageService minioService,
        ILogger<PdfGenerationController> logger)
    {
        _db = db;
        _pdfService = pdfService;
        _minioService = minioService;
        _logger = logger;
    }

    // POST: api/pdf/generate/invoice/{id}
    [HttpPost("generate/invoice/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GenerateInvoicePdf(int id, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .Include(i => i.Organization)
                .ThenInclude(o => o!.Address)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return NotFound(new { message = $"Invoice {id} not found" });

        var bankAccounts = await _db.BankAccounts
            .Where(b => b.OrganizationId == invoice.OrganizationId && b.Active)
            .ToListAsync(ct);

        var pdfBytes = await _pdfService.GeneratePdfFromInvoiceAsync(invoice, bankAccounts, invoice.TemplateId);
        var storageKey = await _minioService.UploadPdfAsync(invoice.Id, pdfBytes);

        invoice.PdfStorageKey = storageKey;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Generated PDF for Invoice {InvoiceId} → {StorageKey}", id, storageKey);
        return Ok(new { storageKey });
    }

    // POST: api/pdf/generate/quote/{id}
    [HttpPost("generate/quote/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GenerateQuotePdf(int id, CancellationToken ct)
    {
        var quote = await _db.Quotes
            .Include(q => q.Items)
            .Include(q => q.Client)
            .Include(q => q.Organization)
                .ThenInclude(o => o!.Address)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        if (quote == null)
            return NotFound(new { message = $"Quote {id} not found" });

        var pdfBytes = await _pdfService.GeneratePdfFromQuoteAsync(quote, quote.TemplateId);
        var storageKey = await _minioService.UploadQuotePdfAsync(quote.Id, pdfBytes);

        quote.PdfStorageKey = storageKey;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Generated PDF for Quote {QuoteId} → {StorageKey}", id, storageKey);
        return Ok(new { storageKey });
    }
}
