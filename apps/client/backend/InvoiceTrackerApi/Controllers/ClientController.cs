using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.DTOs.Client.Responses;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.Services.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

/// <summary>
/// Client management endpoints
/// </summary>
[ApiController]
[Route("api/client")]
[Produces("application/json")]
[Authorize]
public class ClientController : AuthenticatedControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<ClientController> _logger;

    public ClientController(IClientService clientService, ILogger<ClientController> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of clients with optional search
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="search">Optional search term for name, email, or VAT number</param>
    /// <returns>Paginated list of clients</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<ClientResponse>>> GetClients(
        [FromQuery] int organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var response = await _clientService.GetClientsAsync(organizationId, page, pageSize, search);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific client by ID
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <returns>Client details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClientResponse>> GetClient(int id)
    {
        var client = await _clientService.GetClientByIdAsync(id);
        return Ok(client);
    }

    /// <summary>
    /// Create a new client
    /// </summary>
    /// <param name="request">Client creation data</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Created client</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClientResponse>> CreateClient(
        [FromBody] CreateClientRequest request,
        [FromQuery] int organizationId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = GetCurrentUserIdentifier();

        var client = await _clientService.CreateClientAsync(request, organizationId, userEmail);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    /// <summary>
    /// Update an existing client
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="request">Updated client data</param>
    /// <returns>Updated client</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClientResponse>> UpdateClient(int id, [FromBody] UpdateClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userEmail = GetCurrentUserIdentifier();

        var client = await _clientService.UpdateClientAsync(id, request, userEmail);

        return Ok(client);
    }

    /// <summary>
    /// Delete a client
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteClient(int id)
    {
        await _clientService.DeleteClientAsync(id);
        return NoContent();
    }
}
