using InvoiceTrackerApi.Models;

namespace InvoiceTrackerApi.Repositories;

/// <summary>
/// Repository interface for Template data access
/// </summary>
public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(int id);
    Task<Template?> GetByNameAndVersionAsync(string name, int version);
    Task<IEnumerable<Template>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<Template> AddAsync(Template template);
    Task DeleteAsync(Template template);
    Task<bool> ExistsAsync(int id);
}
