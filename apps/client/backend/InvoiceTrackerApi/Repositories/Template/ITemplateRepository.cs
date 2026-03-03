using Shared.Database.Models;
using TemplateModel = Shared.Database.Models.Template;

namespace InvoiceTrackerApi.Repositories.Template;

/// <summary>
/// Repository interface for Template data access
/// </summary>
public interface ITemplateRepository : IRepository<TemplateModel>
{
    Task<TemplateModel?> GetByNameAndVersionAsync(string name, int version);
    Task<TemplateModel?> GetByNameAndVersionAndOrgAsync(string name, int version, int organizationId);
    Task<IEnumerable<TemplateModel>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<TemplateModel>> GetAllByOrganizationAsync(int organizationId, int page, int pageSize, string? search = null, TemplateType? type = null);
    Task<IEnumerable<TemplateModel>> GetByOrganizationAndTypeAsync(int organizationId, TemplateType type);
    Task<int> GetTotalCountAsync();
    Task<int> GetTotalCountByOrganizationAsync(int organizationId, string? search = null, TemplateType? type = null);
}
