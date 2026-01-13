using AuthApi.DTOs;
using AuthApi.Models;
using AuthApi.Services.Quote;
using AuthApi.Services.Template;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/quote")]
[Produces("application/json")]
[Authorize]
public class QuoteController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<QuoteController> _logger;

    public QuoteController(
        IQuoteService quoteService,
        ITemplateService templateService,
        ILogger<QuoteController> logger)
    {
        _quoteService = quoteService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get available quote templates
    /// </summary>
    /// <returns>List of template names</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> GetTemplates()
    {
        var templates = await _templateService.GetQuoteTemplateNamesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Get paginated list of quotes
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <returns>Paginated list of quotes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<Quote>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<Quote>>> GetQuotes(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var response = await _quoteService.GetQuotesAsync(page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get presigned URL for quote PDF
    /// </summary>
    /// <param name="id">Quote ID</param>
    /// <returns>Presigned URL for PDF download</returns>
    [HttpGet("{id}/pdf-url")]
    [ProducesResponseType(typeof(PdfUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PdfUrlResponse>> GetQuotePdfUrl(int id)
    {
        var url = await _quoteService.GetQuotePdfUrlAsync(id);
        return Ok(new PdfUrlResponse { Url = url ?? "" });
    }

    /// <summary>
    /// Get a specific quote by ID
    /// </summary>
    /// <param name="id">Quote ID</param>
    /// <returns>Quote details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Quote), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Quote>> GetQuote(int id)
    {
        var quote = await _quoteService.GetQuoteByIdAsync(id);
        return Ok(quote);
    }

    /// <summary>
    /// Create a new quote
    /// </summary>
    /// <param name="request">Quote creation data</param>
    /// <returns>Created quote</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Quote), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Quote>> CreateQuote([FromBody] CreateQuoteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        var quote = await _quoteService.CreateQuoteAsync(request, userEmail);

        return CreatedAtAction(nameof(GetQuote), new { id = quote.Id }, quote);
    }

    /// <summary>
    /// Update an existing quote
    /// </summary>
    /// <param name="id">Quote ID</param>
    /// <param name="request">Updated quote data</param>
    /// <returns>Updated quote</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Quote), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Quote>> UpdateQuote(int id, [FromBody] UpdateQuoteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        var quote = await _quoteService.UpdateQuoteAsync(id, request, userEmail);

        return Ok(quote);
    }

    /// <summary>
    /// Delete a quote
    /// </summary>
    /// <param name="id">Quote ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteQuote(int id)
    {
        await _quoteService.DeleteQuoteAsync(id);
        return NoContent();
    }
}
