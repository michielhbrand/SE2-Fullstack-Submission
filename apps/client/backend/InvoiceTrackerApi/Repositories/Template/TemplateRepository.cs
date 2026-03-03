using Shared.Database.Data;
using Shared.Database.Models;
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

    public async Task<TemplateModel?> GetByNameAndVersionAndOrgAsync(string name, int version, int organizationId)
    {
        return await _context.Templates
            .FirstOrDefaultAsync(t => t.Name == name && t.Version == version && t.OrganizationId == organizationId);
    }

    public async Task<IEnumerable<TemplateModel>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Templates
            .OrderByDescending(t => t.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<TemplateModel>> GetAllByOrganizationAsync(int organizationId, int page, int pageSize, string? search = null, TemplateType? type = null)
    {
        var query = _context.Templates.Where(t => t.OrganizationId == organizationId || t.OrganizationId == null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()));

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return await query
            .OrderByDescending(t => t.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<TemplateModel>> GetByOrganizationAndTypeAsync(int organizationId, TemplateType type)
    {
        return await _context.Templates
            .Where(t => t.Type == type && (t.OrganizationId == organizationId || t.OrganizationId == null))
            .OrderByDescending(t => t.Created)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Templates.CountAsync();
    }

    public async Task<int> GetTotalCountByOrganizationAsync(int organizationId, string? search = null, TemplateType? type = null)
    {
        var query = _context.Templates.Where(t => t.OrganizationId == organizationId || t.OrganizationId == null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()));

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return await query.CountAsync();
    }
}
