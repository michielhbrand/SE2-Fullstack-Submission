using InvoiceTrackerApi.Models;
using ClientModel = InvoiceTrackerApi.Models.Client;

namespace InvoiceTrackerApi.Repositories.Client;

/// <summary>
/// Repository interface for Client data access
/// </summary>
public interface IClientRepository : IRepository<ClientModel>
{
    Task<ClientModel?> GetByEmailAsync(string email);
    Task<IEnumerable<ClientModel>> GetAllAsync(int page, int pageSize, string? search = null);
    Task<int> GetTotalCountAsync(string? search = null);
}
