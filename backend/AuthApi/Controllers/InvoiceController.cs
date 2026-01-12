using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceController> _logger;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public InvoiceController(
        ApplicationDbContext context,
        ILogger<InvoiceController> logger,
        IKafkaProducerService kafkaProducer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    // GET: api/Invoice/templates
    [HttpGet("templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> GetTemplates()
    {
        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var response = await _httpClient.GetAsync($"{pdfServiceUrl}/api/Template");
            
            if (response.IsSuccessStatusCode)
            {
                var templates = await response.Content.ReadFromJsonAsync<List<string>>();
                return Ok(templates ?? new List<string>());
            }
            
            _logger.LogWarning("Failed to fetch templates from PDF Generator Service. Status: {Status}", response.StatusCode);
            return Ok(new List<string>()); // Return empty list on failure
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates from PDF Generator Service");
            return StatusCode(500, new { message = "Error fetching templates" });
        }
    }

    // GET: api/Invoice
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<Invoice>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<Invoice>>> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size limit

        var totalCount = await _context.Invoices.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var invoices = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var response = new PaginatedResponse<Invoice>
        {
            Data = invoices,
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };

        return Ok(response);
    }

    // GET: api/Invoice/{id}/pdf-url
    [HttpGet("{id}/pdf-url")]
    [ProducesResponseType(typeof(PdfUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PdfUrlResponse>> GetInvoicePdfUrl(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);

        if (invoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        if (string.IsNullOrEmpty(invoice.PdfStorageKey))
        {
            return NotFound(new { message = "PDF not yet generated for this invoice" });
        }

        try
        {
            // Call PdfGeneratorService to get presigned URL
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var response = await _httpClient.GetAsync($"{pdfServiceUrl}/api/Pdf/presigned-url?storageKey={Uri.EscapeDataString(invoice.PdfStorageKey)}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return Ok(new PdfUrlResponse { Url = result?["url"] ?? "" });
            }
            
            _logger.LogWarning("Failed to get presigned URL from PDF Generator Service. Status: {Status}", response.StatusCode);
            return StatusCode(500, new { message = "Failed to generate PDF URL" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF URL for Invoice {InvoiceId}", id);
            return StatusCode(500, new { message = "Error generating PDF URL" });
        }
    }

    // GET: api/Invoice/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Invoice>> GetInvoice(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        return Ok(invoice);
    }

    // POST: api/Invoice
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Invoice>> CreateInvoice(Invoice invoice)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Set creation date
        invoice.DateCreated = DateTime.UtcNow;
        
        // Get user email from claims
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";
        
        invoice.ModifiedBy = userEmail;
        invoice.LastModifiedDate = DateTime.UtcNow;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} created by {User}", invoice.Id, userEmail);

        // Publish Kafka event for PDF generation
        try
        {
            await _kafkaProducer.PublishInvoiceCreatedEventAsync(invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Kafka event for Invoice {InvoiceId}", invoice.Id);
            // Continue - invoice is created, PDF generation will be retried
        }

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    // PUT: api/Invoice/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateInvoice(int id, Invoice invoice)
    {
        if (id != invoice.Id)
        {
            return BadRequest(new { message = "Invoice ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingInvoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (existingInvoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        // Get user email from claims
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        // Update properties
        existingInvoice.ClientId = invoice.ClientId;
        existingInvoice.NotificationSent = invoice.NotificationSent;
        existingInvoice.LastModifiedDate = DateTime.UtcNow;
        existingInvoice.ModifiedBy = userEmail;

        // Update items - remove old items and add new ones
        _context.InvoiceItems.RemoveRange(existingInvoice.Items);
        existingInvoice.Items = invoice.Items;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Invoice {InvoiceId} updated by {User}", id, userEmail);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!InvoiceExists(id))
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }
            throw;
        }

        return Ok(existingInvoice);
    }

    // DELETE: api/Invoice/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} deleted by {User}", id, userEmail);

        return NoContent();
    }

    private bool InvoiceExists(int id)
    {
        return _context.Invoices.Any(e => e.Id == id);
    }
}
