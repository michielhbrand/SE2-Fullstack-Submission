using InvoiceTrackerApi.Models;

namespace InvoiceTrackerApi.Repositories.Organization;

/// <summary>
/// Repository interface for Organization data access
/// </summary>
public interface IOrganizationRepository : IRepository<Models.Organization>
{
    Task<IEnumerable<Models.Organization>> GetAllAsync();
    Task<Models.Organization?> GetByIdWithDetailsAsync(int id);
}
