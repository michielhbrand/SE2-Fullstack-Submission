using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Controllers;

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

    // GET: api/Client
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetClients([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size limit

        var query = _context.Clients.AsQueryable();

        // Apply search filter if provided
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
        
        var response = new
        {
            data = clients,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalCount,
                totalPages
            }
        };

        return Ok(response);
    }

    // GET: api/Client/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Client>> GetClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);

        if (client == null)
        {
            return NotFound(new { message = $"Client with ID {id} not found" });
        }

        return Ok(client);
    }

    // POST: api/Client
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Client>> CreateClient(Client client)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if email already exists
        if (await _context.Clients.AnyAsync(c => c.Email == client.Email))
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }

        // Set creation date
        client.DateCreated = DateTime.UtcNow;
        
        // Get user email from claims
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";
        
        client.ModifiedBy = userEmail;
        client.LastModifiedDate = DateTime.UtcNow;

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client {ClientId} created by {User}", client.Id, userEmail);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    // PUT: api/Client/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateClient(int id, Client client)
    {
        if (id != client.Id)
        {
            return BadRequest(new { message = "Client ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingClient = await _context.Clients.FindAsync(id);

        if (existingClient == null)
        {
            return NotFound(new { message = $"Client with ID {id} not found" });
        }

        // Check if email is being changed to one that already exists
        if (existingClient.Email != client.Email && 
            await _context.Clients.AnyAsync(c => c.Email == client.Email))
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }

        // Get user email from claims
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        // Update properties
        existingClient.Name = client.Name;
        existingClient.Surname = client.Surname;
        existingClient.Email = client.Email;
        existingClient.Cellphone = client.Cellphone;
        existingClient.Address = client.Address;
        existingClient.Company = client.Company;
        existingClient.KeycloakUserId = client.KeycloakUserId;
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

        return Ok(existingClient);
    }

    // DELETE: api/Client/5
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
