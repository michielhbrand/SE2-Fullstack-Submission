using InvoiceTrackerApi.Data;
using Microsoft.EntityFrameworkCore;
using InvoiceModel = InvoiceTrackerApi.Models.Invoice;

namespace InvoiceTrackerApi.Repositories.Invoice;

/// <summary>
/// Repository implementation for Invoice data access
/// </summary>
public class InvoiceRepository : Repository<InvoiceModel>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
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
}
