using QuoteModel = Shared.Database.Models.Quote;

namespace InvoiceTrackerApi.Repositories.Quote;

/// <summary>
/// Repository interface for Quote data access
/// </summary>
public interface IQuoteRepository : IRepository<QuoteModel>
{
    Task<QuoteModel?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<QuoteModel>> GetAllAsync(int organizationId, int page, int pageSize);
    Task<int> GetTotalCountAsync(int organizationId);
}
