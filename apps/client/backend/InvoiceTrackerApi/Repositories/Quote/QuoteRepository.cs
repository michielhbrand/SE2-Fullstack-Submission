using Shared.Database.Data;
using Microsoft.EntityFrameworkCore;
using QuoteModel = Shared.Database.Models.Quote;

namespace InvoiceTrackerApi.Repositories.Quote;

/// <summary>
/// Repository implementation for Quote data access
/// </summary>
public class QuoteRepository : Repository<QuoteModel>, IQuoteRepository
{
    public QuoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<QuoteModel?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Quotes
            .Include(q => q.Items)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<IEnumerable<QuoteModel>> GetAllAsync(int organizationId, int page, int pageSize)
    {
        return await _context.Quotes
            .Include(q => q.Items)
            .Include(q => q.Client)
            .Where(q => q.OrganizationId == organizationId)
            .OrderByDescending(q => q.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(int organizationId)
    {
        return await _context.Quotes
            .Where(q => q.OrganizationId == organizationId)
            .CountAsync();
    }
}
