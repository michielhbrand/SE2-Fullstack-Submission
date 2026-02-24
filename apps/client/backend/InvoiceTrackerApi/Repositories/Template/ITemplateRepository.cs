using TemplateModel = Shared.Database.Models.Template;

namespace InvoiceTrackerApi.Repositories.Template;

/// <summary>
/// Repository interface for Template data access
/// </summary>
public interface ITemplateRepository : IRepository<TemplateModel>
{
    Task<TemplateModel?> GetByNameAndVersionAsync(string name, int version);
    Task<IEnumerable<TemplateModel>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
}
