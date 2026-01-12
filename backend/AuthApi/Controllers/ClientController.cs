using AuthApi.Data;
using AuthApi.Models;
using AuthApi.DTOs;
using AuthApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers;

/// <summary>
/// Client management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClientController> _logger;

    public ClientController(ApplicationDbContext context, ILogger<ClientController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of clients with optional search
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="search">Optional search term for name, surname, email, or company</param>
    /// <returns>Paginated list of clients</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<ClientDto>>> GetClients(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(search) ||
                c.Surname.ToLower().Contains(search) ||
                c.Email.ToLower().Contains(search) ||
                c.Company != null && c.Company.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var clients = await query
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var response = new PaginatedResponse<ClientDto>
        {
            Data = clients.Select(c => c.ToDto()).ToList(),
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

    /// <summary>
    /// Get a specific client by ID
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <returns>Client details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClientDto>> GetClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);

        if (client == null)
        {
            return NotFound(new { message = $"Client with ID {id} not found" });
        }

        return Ok(client.ToDto());
    }

    /// <summary>
    /// Create a new client
    /// </summary>
    /// <param name="request">Client creation data</param>
    /// <returns>Created client</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await _context.Clients.AnyAsync(c => c.Email == request.Email))
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }

        var client = request.ToModel();
        client.DateCreated = DateTime.UtcNow;
        
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";
        
        client.ModifiedBy = userEmail;
        client.LastModifiedDate = DateTime.UtcNow;

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} created by {User}", client.Id, userEmail);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client.ToDto());
    }

    /// <summary>
    /// Update an existing client
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="request">Updated client data</param>
    /// <returns>Updated client</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClientDto>> UpdateClient(int id, [FromBody] UpdateClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingClient = await _context.Clients.FindAsync(id);

        if (existingClient == null)
        {
            return NotFound(new { message = $"Client with ID {id} not found" });
        }

        if (existingClient.Email != request.Email && 
            await _context.Clients.AnyAsync(c => c.Email == request.Email))
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        existingClient.Name = request.Name;
        existingClient.Surname = request.Surname;
        existingClient.Email = request.Email;
        existingClient.Cellphone = request.Cellphone;
        existingClient.Address = request.Address;
        existingClient.Company = request.Company;
        existingClient.KeycloakUserId = request.KeycloakUserId;
        existingClient.LastModifiedDate = DateTime.UtcNow;
        existingClient.ModifiedBy = userEmail;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Client {ClientId} updated by {User}", id, userEmail);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClientExists(id))
            {
                return NotFound(new { message = $"Client with ID {id} not found" });
            }
            throw;
        }

        return Ok(existingClient.ToDto());
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
        var client = await _context.Clients.FindAsync(id);

        if (client == null)
        {
            return NotFound(new { message = $"Client with ID {id} not found" });
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} deleted by {User}", id, userEmail);

        return NoContent();
    }

    private bool ClientExists(int id)
    {
        return _context.Clients.Any(e => e.Id == id);
    }
}
