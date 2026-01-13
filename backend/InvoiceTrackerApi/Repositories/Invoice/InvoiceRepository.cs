using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.Models;
using Microsoft.EntityFrameworkCore;
using InvoiceModel = InvoiceTrackerApi.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository implementation for Invoice data access
/// </summary>
public class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _context;

    public InvoiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceModel?> GetByIdAsync(int id)
    {
        return await _context.Invoices.FindAsync(id);
    }

    public async Task<InvoiceModel?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InvoiceModel>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Client)
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Invoices.CountAsync();
    }

    public async Task<InvoiceModel> AddAsync(InvoiceModel invoice)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task UpdateAsync(InvoiceModel invoice)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(InvoiceModel invoice)
    {
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Invoices.AnyAsync(i => i.Id == id);
    }
}
