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
public class InvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(ApplicationDbContext context, ILogger<InvoiceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Invoice
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size limit

        var totalCount = await _context.Invoices.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var invoices = await _context.Invoices
            .Include(i => i.Items)
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var response = new
        {
            data = invoices,
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

    // GET: api/Invoice/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Invoice>> GetInvoice(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        return Ok(invoice);
    }

    // POST: api/Invoice
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Invoice>> CreateInvoice(Invoice invoice)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Set creation date
        invoice.DateCreated = DateTime.UtcNow;
        
        // Get user email from claims
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";
        
        invoice.ModifiedBy = userEmail;
        invoice.LastModifiedDate = DateTime.UtcNow;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} created by {User}", invoice.Id, userEmail);

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    // PUT: api/Invoice/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateInvoice(int id, Invoice invoice)
    {
        if (id != invoice.Id)
        {
            return BadRequest(new { message = "Invoice ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingInvoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (existingInvoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        // Get user email from claims
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        // Update properties
        existingInvoice.ClientName = invoice.ClientName;
        existingInvoice.ClientSurname = invoice.ClientSurname;
        existingInvoice.ClientAddress = invoice.ClientAddress;
        existingInvoice.ClientCellphone = invoice.ClientCellphone;
        existingInvoice.NotificationSent = invoice.NotificationSent;
        existingInvoice.LastModifiedDate = DateTime.UtcNow;
        existingInvoice.ModifiedBy = userEmail;

        // Update items - remove old items and add new ones
        _context.InvoiceItems.RemoveRange(existingInvoice.Items);
        existingInvoice.Items = invoice.Items;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Invoice {InvoiceId} updated by {User}", id, userEmail);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!InvoiceExists(id))
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }
            throw;
        }

        return Ok(existingInvoice);
    }

    // DELETE: api/Invoice/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = $"Invoice with ID {id} not found" });
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                       ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                       ?? "system";

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} deleted by {User}", id, userEmail);

        return NoContent();
    }

    private bool InvoiceExists(int id)
    {
        return _context.Invoices.Any(e => e.Id == id);
    }
}
