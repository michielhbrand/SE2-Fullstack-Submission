using AuthApi.Models;

namespace AuthApi.Repositories;

/// <summary>
/// Repository interface for Invoice data access
/// </summary>
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Invoice>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<Invoice> AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(Invoice invoice);
    Task<bool> ExistsAsync(int id);
}
