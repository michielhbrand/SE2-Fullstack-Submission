using AuthApi.Models;

namespace AuthApi.Repositories;

/// <summary>
/// Repository interface for Quote data access
/// </summary>
public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(int id);
    Task<Quote?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Quote>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<Quote> AddAsync(Quote quote);
    Task UpdateAsync(Quote quote);
    Task DeleteAsync(Quote quote);
    Task<bool> ExistsAsync(int id);
}
