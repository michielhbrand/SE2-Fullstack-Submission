using Shared.Database.Data;
using Microsoft.EntityFrameworkCore;
using TemplateModel = Shared.Database.Models.Template;

namespace InvoiceTrackerApi.Repositories.Template;

/// <summary>
/// Repository implementation for Template data access
/// </summary>
public class TemplateRepository : Repository<TemplateModel>, ITemplateRepository
{
    public TemplateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TemplateModel?> GetByNameAndVersionAsync(string name, int version)
    {
        return await _context.Templates
            .FirstOrDefaultAsync(t => t.Name == name && t.Version == version);
    }

    public async Task<IEnumerable<TemplateModel>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Templates
            .OrderByDescending(t => t.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Templates.CountAsync();
    }
}
