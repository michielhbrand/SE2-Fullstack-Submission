using InvoiceTrackerApi.Models;
using QuoteModel = InvoiceTrackerApi.Models.Quote;

namespace InvoiceTrackerApi.Repositories.Quote;

/// <summary>
/// Repository interface for Quote data access
/// </summary>
public interface IQuoteRepository
{
    Task<QuoteModel?> GetByIdAsync(int id);
    Task<QuoteModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<QuoteModel>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<QuoteModel> AddAsync(QuoteModel quote);
    Task UpdateAsync(QuoteModel quote);
    Task DeleteAsync(QuoteModel quote);
    Task<bool> ExistsAsync(int id);
}
