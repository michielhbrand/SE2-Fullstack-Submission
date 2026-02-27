using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.Services.Storage;
using PdfGeneratorService.Services.Generation;
using Shared.Database.Data;
using Shared.Database.Models;

namespace PdfGeneratorService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplateController : ControllerBase
{
    private readonly IMinioStorageService _storageService;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        IMinioStorageService storageService,
        IPdfGenerationService pdfGenerationService,
        ApplicationDbContext db,
        ILogger<TemplateController> logger)
    {
        _storageService = storageService;
        _pdfGenerationService = pdfGenerationService;
        _db = db;
        _logger = logger;
    }

    // GET: api/Template
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetTemplates()
    {
        try
        {
            var templates = await _storageService.ListTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice templates");
            return StatusCode(500, new { message = "Error retrieving invoice templates" });
        }
    }

    // GET: api/Template/quote-templates
    [HttpGet("quote-templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetQuoteTemplates()
    {
        try
        {
            var templates = await _storageService.ListQuoteTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quote templates");
            return StatusCode(500, new { message = "Error retrieving quote templates" });
        }
    }

    // GET: api/Template/{templateName}
    [HttpGet("{templateName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetTemplate(string templateName)
    {
        try
        {
            var template = await _storageService.GetTemplateAsync(templateName);
            return Ok(new { name = templateName, content = template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateName}", templateName);
            return NotFound(new { message = $"Template '{templateName}' not found" });
        }
    }

    // POST: api/Template
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UploadTemplate([FromBody] UploadTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Template name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Template content is required" });
            }

            var templateName = await _storageService.UploadTemplateAsync(request.Name, request.Content);
            return CreatedAtAction(nameof(GetTemplate), new { templateName }, new { name = templateName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template");
            return StatusCode(500, new { message = "Error uploading template" });
        }
    }

    // GET: api/Template/{templateId}/preview
    [HttpGet("{templateId:int}/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PreviewTemplate(int templateId)
    {
        try
        {
            var template = await _db.Templates.FirstOrDefaultAsync(t => t.Id == templateId);
            if (template == null)
                return NotFound(new { message = $"Template with ID '{templateId}' not found" });

            var sampleClient = new Client
            {
                Id = 1,
                Name = "John Doe",
                Email = "john.doe@example.com",
                Address = "123 Sample Street, City, State 12345",
                Cellphone = "+1 (555) 123-4567",
                IsCompany = false,
                DateCreated = DateTime.UtcNow
            };

            var sampleItems = new List<InvoiceItem>
            {
                new() { Description = "Sample Product 1",  Quantity = 2, PricePerUnit = 50.00m  },
                new() { Description = "Sample Service",     Quantity = 1, PricePerUnit = 150.00m },
                new() { Description = "Sample Product 2",  Quantity = 3, PricePerUnit = 25.00m  },
            };

            byte[] pdfBytes;

            if (template.Type == TemplateType.Invoice)
            {
                var sampleInvoice = new Invoice
                {
                    Id = 1,
                    ClientId = 1,
                    DateCreated = DateTime.UtcNow,
                    PayByDate = DateTime.UtcNow.AddDays(30),
                    Client = sampleClient,
                    Items = sampleItems,
                };
                pdfBytes = await _pdfGenerationService.GeneratePdfFromInvoiceAsync(sampleInvoice, new List<BankAccount>(), templateId);
            }
            else
            {
                var sampleQuote = new Quote
                {
                    Id = 1,
                    ClientId = 1,
                    DateCreated = DateTime.UtcNow,
                    Client = sampleClient,
                    Items = sampleItems.Select(i => new QuoteItem
                    {
                        Description = i.Description,
                        Quantity = i.Quantity,
                        PricePerUnit = i.PricePerUnit,
                    }).ToList(),
                };
                pdfBytes = await _pdfGenerationService.GeneratePdfFromQuoteAsync(sampleQuote, templateId);
            }

            // Return inline (no filename) so the browser renders the PDF in the iframe
            return File(pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for template {TemplateId}", templateId);
            return NotFound(new { message = $"Template with ID '{templateId}' not found or error generating preview" });
        }
    }
}

public record UploadTemplateRequest(string Name, string Content);
