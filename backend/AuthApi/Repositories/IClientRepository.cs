using AuthApi.Models;

namespace AuthApi.Repositories;

/// <summary>
/// Repository interface for Client data access
/// </summary>
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(int id);
    Task<Client?> GetByEmailAsync(string email);
    Task<IEnumerable<Client>> GetAllAsync(int page, int pageSize, string? search = null);
    Task<int> GetTotalCountAsync(string? search = null);
    Task<Client> AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Client client);
    Task<bool> ExistsAsync(int id);
}
