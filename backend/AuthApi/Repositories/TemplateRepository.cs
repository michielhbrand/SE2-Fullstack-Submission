using AuthApi.Data;
using AuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Repositories;

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

    public async Task<Template?> GetByIdAsync(int id)
    {
        return await _context.Templates.FindAsync(id);
    }

    public async Task<Template?> GetByNameAndVersionAsync(string name, int version)
    {
        return await _context.Templates
            .FirstOrDefaultAsync(t => t.Name == name && t.Version == version);
    }

    public async Task<IEnumerable<Template>> GetAllAsync(int page, int pageSize)
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

    public async Task<Template> AddAsync(Template template)
    {
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(Template template)
    {
        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Templates.AnyAsync(t => t.Id == id);
    }
}
