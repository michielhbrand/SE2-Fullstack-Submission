using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.DTOs.Responses;
using InvoiceTrackerApi.Models;
using QuoteModel = InvoiceTrackerApi.Models.Quote;

namespace InvoiceTrackerApi.Services.Quote;

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
