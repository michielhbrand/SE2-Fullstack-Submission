using InvoiceTrackerApi.Models;
using ClientModel = InvoiceTrackerApi.Models.Client;

namespace InvoiceTrackerApi.Repositories.Client;

/// <summary>
/// Repository interface for Client data access
/// </summary>
public interface IClientRepository
{
    Task<ClientModel?> GetByIdAsync(int id);
    Task<ClientModel?> GetByEmailAsync(string email);
    Task<IEnumerable<ClientModel>> GetAllAsync(int page, int pageSize, string? search = null);
    Task<int> GetTotalCountAsync(string? search = null);
    Task<ClientModel> AddAsync(ClientModel client);
    Task UpdateAsync(ClientModel client);
    Task DeleteAsync(ClientModel client);
    Task<bool> ExistsAsync(int id);
}
