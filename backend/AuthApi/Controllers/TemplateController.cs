using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.Models;
using System.Security.Claims;

namespace AuthApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplateController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TemplateController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public TemplateController(
        ApplicationDbContext context,
        ILogger<TemplateController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // GET: api/Template
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Template>>> GetTemplates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var query = _context.Templates
                .OrderByDescending(t => t.Created)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var templates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                data = templates,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalPages,
                    totalCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return StatusCode(500, new { message = "Error retrieving templates" });
        }
    }

    // GET: api/Template/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Template>> GetTemplate(int id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Template with ID {id} not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
            return StatusCode(500, new { message = "Error retrieving template" });
        }
    }

    // POST: api/Template
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Template>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            // Get the current user ID from the JWT token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value 
                ?? "unknown";

            // Check if a template with the same name and version already exists
            var existingTemplate = await _context.Templates
                .FirstOrDefaultAsync(t => t.Name == request.Name && t.Version == request.Version);

            if (existingTemplate != null)
            {
                return Conflict(new { message = $"A template with name '{request.Name}' and version {request.Version} already exists" });
            }

            // Upload the template HTML to PdfGeneratorService
            var httpClient = _httpClientFactory.CreateClient();
            var uploadResponse = await httpClient.PostAsJsonAsync(
                "http://localhost:5001/api/Template",
                new { name = request.Name, content = request.Content });

            if (!uploadResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to upload template to PdfGeneratorService");
                return StatusCode(500, new { message = "Failed to upload template to storage" });
            }

            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadTemplateResponse>();
            var storageKey = $"templates/{uploadResult?.Name}";

            // Create the template record
            var template = new Template
            {
                CreatedBy = userId,
                Created = DateTime.UtcNow,
                Version = request.Version,
                Name = request.Name,
                StorageKey = storageKey
            };

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template created: {TemplateName} v{Version} by user {UserId}",
                template.Name, template.Version, userId);

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Templates_Name_Version") == true)
        {
            return Conflict(new { message = $"A template with name '{request.Name}' and version {request.Version} already exists" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { message = "Error creating template" });
        }
    }

    // DELETE: api/Template/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Template with ID {id} not found" });
            }

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Template deleted: {TemplateId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            return StatusCode(500, new { message = "Error deleting template" });
        }
    }

    // GET: api/Template/{id}/preview-url
    [HttpGet("{id}/preview-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetTemplatePreviewUrl(int id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);

            if (template == null)
            {
                return NotFound(new { message = $"Template with ID {id} not found" });
            }

            // Extract template name from storage key
            var templateName = template.StorageKey.Replace("templates/", "");
            var previewUrl = $"http://localhost:5001/api/Template/{Uri.EscapeDataString(templateName)}/preview";

            return Ok(new { url = previewUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview URL for template {TemplateId}", id);
            return StatusCode(500, new { message = "Error generating preview URL" });
        }
    }
}

public record CreateTemplateRequest(string Name, int Version, string Content);
public record UploadTemplateResponse(string Name);
