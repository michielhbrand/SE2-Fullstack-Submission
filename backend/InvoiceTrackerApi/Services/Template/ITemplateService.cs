using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.DTOs.Responses;
using InvoiceTrackerApi.Models;
using TemplateModel = InvoiceTrackerApi.Models.Template;

namespace InvoiceTrackerApi.Services.Template;

/// <summary>
/// Service interface for Template business logic
/// </summary>
public interface ITemplateService
{
    Task<PaginatedResponse<TemplateResponse>> GetTemplatesAsync(int page, int pageSize);
    Task<TemplateModel> GetTemplateByIdAsync(int id);
    Task<TemplateModel> CreateTemplateAsync(CreateTemplateRequest request, string createdBy);
    Task DeleteTemplateAsync(int id);
    Task<string> GetTemplatePreviewUrlAsync(int id);
    Task<List<string>> GetInvoiceTemplateNamesAsync();
    Task<List<string>> GetQuoteTemplateNamesAsync();
}
