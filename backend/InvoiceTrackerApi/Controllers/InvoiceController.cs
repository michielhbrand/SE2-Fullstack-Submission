using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.DTOs.Responses;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Services.Invoice;
using InvoiceTrackerApi.Services.Template;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

[ApiController]
[Route("api/invoice")]
[Produces("application/json")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        ITemplateService templateService,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get available invoice templates
    /// </summary>
    /// <returns>List of template names</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> GetTemplates()
    {
        var templates = await _templateService.GetInvoiceTemplateNamesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Get paginated list of invoices
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <returns>Paginated list of invoices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<Invoice>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<Invoice>>> GetInvoices(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var response = await _invoiceService.GetInvoicesAsync(page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get presigned URL for invoice PDF
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Presigned URL for PDF download</returns>
    [HttpGet("{id}/pdf-url")]
    [ProducesResponseType(typeof(PdfUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PdfUrlResponse>> GetInvoicePdfUrl(int id)
    {
        var url = await _invoiceService.GetInvoicePdfUrlAsync(id);
        return Ok(new PdfUrlResponse { Url = url ?? "" });
    }

    /// <summary>
    /// Get a specific invoice by ID
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Invoice details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(int id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        return Ok(invoice);
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    /// <param name="request">Invoice creation data</param>
    /// <returns>Created invoice</returns>
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InvoiceResponse>> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        var invoice = await _invoiceService.CreateInvoiceAsync(request, userEmail);

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    /// <summary>
    /// Update an existing invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="request">Updated invoice data</param>
    /// <returns>Updated invoice</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InvoiceResponse>> UpdateInvoice(int id, [FromBody] UpdateInvoiceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        var invoice = await _invoiceService.UpdateInvoiceAsync(id, request, userEmail);

        return Ok(invoice);
    }

    /// <summary>
    /// Delete an invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        await _invoiceService.DeleteInvoiceAsync(id);
        return NoContent();
    }
}
