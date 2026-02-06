using InvoiceTrackerApi.DTOs.Quote.Requests;
using InvoiceTrackerApi.DTOs.Quote.Responses;
using InvoiceTrackerApi.DTOs.Common;

namespace InvoiceTrackerApi.Services.Quote;

/// <summary>
/// Service interface for Quote business logic
/// </summary>
public interface IQuoteService
{
    Task<PaginatedResponse<QuoteResponse>> GetQuotesAsync(int page, int pageSize);
    Task<QuoteResponse> GetQuoteByIdAsync(int id);
    Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, string modifiedBy);
    Task<QuoteResponse> UpdateQuoteAsync(int id, UpdateQuoteRequest request, string modifiedBy);
    Task DeleteQuoteAsync(int id);
    Task<string?> GetQuotePdfUrlAsync(int id);
}
