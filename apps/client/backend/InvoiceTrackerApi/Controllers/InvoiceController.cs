using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.Services.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

[ApiController]
[Route("api/invoice")]
[Produces("application/json")]
[Authorize]
public class InvoiceController : AuthenticatedControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of invoices
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="overdueOnly">When true, return only overdue unpaid invoices</param>
    /// <returns>Paginated list of invoices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<InvoiceResponse>>> GetInvoices(
        [FromQuery] int organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool overdueOnly = false)
    {
        var response = await _invoiceService.GetInvoicesAsync(organizationId, page, pageSize, overdueOnly);
        return Ok(response);
    }

    /// <summary>
    /// Manually trigger the overdue invoice check for the organisation.
    /// Publishes an invoice-overdue Kafka event for each qualifying invoice.
    /// </summary>
    /// <returns>Number of invoices queued for reminder</returns>
    [HttpPost("process-overdue")]
    [Authorize(Roles = "orgAdmin,systemAdmin")]
    [ProducesResponseType(typeof(ProcessOverdueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProcessOverdueResponse>> ProcessOverdue(
        [FromQuery] int organizationId,
        CancellationToken ct)
    {
        var count = await _invoiceService.ProcessOverdueInvoicesAsync(organizationId, ct);
        return Ok(new ProcessOverdueResponse { QueuedCount = count });
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
    public async Task<ActionResult<InvoiceResponse>> CreateInvoice([FromBody] CreateInvoiceRequest request, [FromQuery] int organizationId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = GetCurrentUserIdentifier();

        var invoice = await _invoiceService.CreateInvoiceAsync(request, userEmail, organizationId);

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

        var userEmail = GetCurrentUserIdentifier();

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

    /// <summary>
    /// Convert a quote into an invoice
    /// </summary>
    /// <param name="request">Quote conversion data</param>
    /// <returns>Created invoice</returns>
    [HttpPost("from-quote")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InvoiceResponse>> ConvertQuoteToInvoice([FromBody] CreateInvoiceFromQuoteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = GetCurrentUserIdentifier();

        // For now, use organizationId from the request or default
        // In a full implementation, this would come from the user's org membership
        var invoice = await _invoiceService.ConvertQuoteToInvoiceAsync(
            new DTOs.Invoice.Requests.ConvertQuoteToInvoiceRequest
            {
                QuoteId = request.QuoteId,
                TemplateId = request.TemplateId,
                PayByDays = request.PayByDays,
                VatInclusive = request.VatInclusive
            },
            userEmail,
            request.OrganizationId);

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }
}

/// <summary>
/// Request body for the from-quote endpoint
/// </summary>
public class CreateInvoiceFromQuoteRequest
{
    public int QuoteId { get; set; }
    public int? TemplateId { get; set; }
    public int OrganizationId { get; set; }
    public int PayByDays { get; set; } = 30;
    public bool? VatInclusive { get; set; }
}
