using Shared.Database.Data;
using InvoiceTrackerApi.Exceptions;
using Microsoft.EntityFrameworkCore;

using ClientModel = Shared.Database.Models.Client;

namespace InvoiceTrackerApi.Repositories.Client;

/// <summary>
/// Repository implementation for Client data access.
/// Wraps database exceptions into application-level exceptions.
/// </summary>
public class ClientRepository : Repository<ClientModel>, IClientRepository
{
    public ClientRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ClientModel?> GetByEmailAsync(string email, int organizationId)
    {
        try
        {
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.Email == email && c.OrganizationId == organizationId);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to retrieve client by email from database", ex);
        }
    }

    public async Task<IEnumerable<ClientModel>> GetAllAsync(int organizationId, int page, int pageSize, string? search = null)
    {
        try
        {
            var query = _context.Clients
                .Where(c => c.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search) ||
                    (c.VatNumber != null && c.VatNumber.ToLower().Contains(search)));
            }

            return await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to retrieve clients from database", ex);
        }
    }

    public async Task<int> GetTotalCountAsync(int organizationId, string? search = null)
    {
        try
        {
            var query = _context.Clients
                .Where(c => c.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search) ||
                    (c.VatNumber != null && c.VatNumber.ToLower().Contains(search)));
            }

            return await query.CountAsync();
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to count clients in database", ex);
        }
    }
}
