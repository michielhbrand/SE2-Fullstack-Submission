using AuthApi.DTOs;
using AuthApi.Models;
using QuoteModel = AuthApi.Models.Quote;

namespace AuthApi.Services.Quote;

/// <summary>
/// Service interface for Quote business logic
/// </summary>
public interface IQuoteService
{
    Task<PaginatedResponse<QuoteModel>> GetQuotesAsync(int page, int pageSize);
    Task<QuoteModel> GetQuoteByIdAsync(int id);
    Task<QuoteModel> CreateQuoteAsync(CreateQuoteRequest request, string modifiedBy);
    Task<QuoteModel> UpdateQuoteAsync(int id, UpdateQuoteRequest request, string modifiedBy);
    Task DeleteQuoteAsync(int id);
    Task<string?> GetQuotePdfUrlAsync(int id);
}
