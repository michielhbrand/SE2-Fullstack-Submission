using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.Models;
using Microsoft.EntityFrameworkCore;
using QuoteModel = InvoiceTrackerApi.Models.Quote;

namespace InvoiceTrackerApi.Repositories.Quote;

/// <summary>
/// Repository implementation for Quote data access
/// </summary>
public class QuoteRepository : IQuoteRepository
{
    private readonly ApplicationDbContext _context;

    public QuoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuoteModel?> GetByIdAsync(int id)
    {
        return await _context.Quotes.FindAsync(id);
    }

    public async Task<QuoteModel?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Quotes
            .Include(q => q.Items)
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<IEnumerable<QuoteModel>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Quotes
            .Include(q => q.Items)
            .Include(q => q.Client)
            .OrderByDescending(q => q.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Quotes.CountAsync();
    }

    public async Task<QuoteModel> AddAsync(QuoteModel quote)
    {
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();
        return quote;
    }

    public async Task UpdateAsync(QuoteModel quote)
    {
        _context.Quotes.Update(quote);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(QuoteModel quote)
    {
        _context.Quotes.Remove(quote);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Quotes.AnyAsync(q => q.Id == id);
    }
}
