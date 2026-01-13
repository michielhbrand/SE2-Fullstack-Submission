using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.DTOs.Responses;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Services.Template;
using System.Security.Claims;

namespace InvoiceTrackerApi.Controllers;

[Authorize]
[ApiController]
[Route("api/template")]
[Produces("application/json")]
public class TemplateController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        ITemplateService templateService,
        ILogger<TemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of templates
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <returns>Paginated list of templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<TemplateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TemplateResponse>>> GetTemplates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await _templateService.GetTemplatesAsync(page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Template details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateResponse>> GetTemplate(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        return Ok(template);
    }

    /// <summary>
    /// Create a new template
    /// </summary>
    /// <param name="request">Template creation data</param>
    /// <returns>Created template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TemplateResponse>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value 
            ?? "unknown";

        var template = await _templateService.CreateTemplateAsync(request, userId);

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        await _templateService.DeleteTemplateAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get preview URL for a template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>Preview URL</returns>
    [HttpGet("{id}/preview-url")]
    [ProducesResponseType(typeof(TemplatePreviewUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplatePreviewUrlResponse>> GetTemplatePreviewUrl(int id)
    {
        var url = await _templateService.GetTemplatePreviewUrlAsync(id);
        return Ok(new TemplatePreviewUrlResponse { Url = url });
    }
}
