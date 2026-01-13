using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.Models;
using Microsoft.EntityFrameworkCore;

using ClientModel = InvoiceTrackerApi.Models.Client;

namespace InvoiceTrackerApi.Repositories.Client;

/// <summary>
/// Repository implementation for Client data access
/// </summary>
public class ClientRepository : Repository<ClientModel>, IClientRepository
{
    public ClientRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ClientModel?> GetByEmailAsync(string email)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<IEnumerable<ClientModel>> GetAllAsync(int page, int pageSize, string? search = null)
    {
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

        return await query
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null)
    {
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

        return await query.CountAsync();
    }
}
