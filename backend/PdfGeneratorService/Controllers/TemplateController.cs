using Microsoft.AspNetCore.Mvc;
using PdfGeneratorService.Services.Storage;

namespace PdfGeneratorService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplateController : ControllerBase
{
    private readonly IMinioStorageService _storageService;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        IMinioStorageService storageService,
        ILogger<TemplateController> logger)
    {
        _storageService = storageService;
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
            _logger.LogError(ex, "Error retrieving templates");
            return StatusCode(500, new { message = "Error retrieving templates" });
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
}

public record UploadTemplateRequest(string Name, string Content);
