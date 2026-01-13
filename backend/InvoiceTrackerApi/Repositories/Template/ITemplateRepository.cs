using InvoiceTrackerApi.Models;
using TemplateModel = InvoiceTrackerApi.Models.Template;

namespace InvoiceTrackerApi.Repositories.Template;

/// <summary>
/// Repository interface for Template data access
/// </summary>
public interface ITemplateRepository
{
    Task<TemplateModel?> GetByIdAsync(int id);
    Task<TemplateModel?> GetByNameAndVersionAsync(string name, int version);
    Task<IEnumerable<TemplateModel>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<TemplateModel> AddAsync(TemplateModel template);
    Task DeleteAsync(TemplateModel template);
    Task<bool> ExistsAsync(int id);
}
