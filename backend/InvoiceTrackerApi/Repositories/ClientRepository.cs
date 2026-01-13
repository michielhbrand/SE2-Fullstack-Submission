using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Repositories;

/// <summary>
/// Repository implementation for Client data access
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;

    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _context.Clients.FindAsync(id);
    }

    public async Task<Client?> GetByEmailAsync(string email)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<IEnumerable<Client>> GetAllAsync(int page, int pageSize, string? search = null)
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

    public async Task<Client> AddAsync(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task UpdateAsync(Client client)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Client client)
    {
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Clients.AnyAsync(c => c.Id == id);
    }
}
