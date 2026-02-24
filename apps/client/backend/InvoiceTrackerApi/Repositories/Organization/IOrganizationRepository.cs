using Shared.Database.Models;

namespace InvoiceTrackerApi.Repositories.Organization;

/// <summary>
/// Repository interface for Organization data access
/// </summary>
public interface IOrganizationRepository : IRepository<Shared.Database.Models.Organization>
{
    Task<IEnumerable<Shared.Database.Models.Organization>> GetAllAsync();
    Task<Shared.Database.Models.Organization?> GetByIdWithDetailsAsync(int id);
}
