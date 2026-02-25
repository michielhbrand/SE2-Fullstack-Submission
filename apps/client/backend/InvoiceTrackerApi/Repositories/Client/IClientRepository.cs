using ClientModel = Shared.Database.Models.Client;

namespace InvoiceTrackerApi.Repositories.Client;

/// <summary>
/// Repository interface for Client data access
/// </summary>
public interface IClientRepository : IRepository<ClientModel>
{
    Task<ClientModel?> GetByEmailAsync(string email, int organizationId);
    Task<IEnumerable<ClientModel>> GetAllAsync(int organizationId, int page, int pageSize, string? search = null);
    Task<int> GetTotalCountAsync(int organizationId, string? search = null);
}
