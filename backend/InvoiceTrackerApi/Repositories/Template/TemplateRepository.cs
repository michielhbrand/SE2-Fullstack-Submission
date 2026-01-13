using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.Models;
using Microsoft.EntityFrameworkCore;
using TemplateModel = InvoiceTrackerApi.Models.Template;

namespace InvoiceTrackerApi.Repositories.Template;

/// <summary>
/// Repository implementation for Template data access
/// </summary>
public class TemplateRepository : ITemplateRepository
{
    private readonly ApplicationDbContext _context;

    public TemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TemplateModel?> GetByIdAsync(int id)
    {
        return await _context.Templates.FindAsync(id);
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

    public async Task<TemplateModel> AddAsync(TemplateModel template)
    {
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(TemplateModel template)
    {
        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Templates.AnyAsync(t => t.Id == id);
    }
}
