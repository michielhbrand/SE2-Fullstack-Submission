using Microsoft.AspNetCore.Mvc;
using PdfGeneratorService.Services.Storage;

namespace PdfGeneratorService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PdfController : ControllerBase
{
    private readonly IMinioStorageService _storageService;
    private readonly ILogger<PdfController> _logger;

    public PdfController(IMinioStorageService storageService, ILogger<PdfController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    // GET: api/Pdf/presigned-url?storageKey=invoices/invoice-1-20260107.pdf
    [HttpGet("presigned-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetPresignedUrl([FromQuery] string storageKey)
    {
        if (string.IsNullOrEmpty(storageKey))
        {
            return BadRequest(new { message = "Storage key is required" });
        }

        try
        {
            var presignedUrl = await _storageService.GetPresignedUrlAsync(storageKey);
            return Ok(new { url = presignedUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for storage key: {StorageKey}", storageKey);
            return StatusCode(500, new { message = "Error generating presigned URL" });
        }
    }
}
