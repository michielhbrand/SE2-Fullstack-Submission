using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Repositories.Organization;

/// <summary>
/// Repository implementation for Organization data access
/// </summary>
public class OrganizationRepository : Repository<Models.Organization>, IOrganizationRepository
{
    public OrganizationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Models.Organization>> GetAllAsync()
    {
        try
        {
            return await _dbSet
                .Include(o => o.Address)
                .ToListAsync();
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to retrieve organizations from database", ex);
        }
    }

    public async Task<Models.Organization?> GetByIdWithDetailsAsync(int id)
    {
        try
        {
            return await _dbSet
                .Include(o => o.Address)
                .FirstOrDefaultAsync(o => o.Id == id);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to retrieve organization from database", ex);
        }
    }
}
