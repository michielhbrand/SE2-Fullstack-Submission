using AuthApi.DTOs;
using AuthApi.Models;
using TemplateModel = AuthApi.Models.Template;

namespace AuthApi.Services.Template;

/// <summary>
/// Service interface for Template business logic
/// </summary>
public interface ITemplateService
{
    Task<PaginatedResponse<TemplateDto>> GetTemplatesAsync(int page, int pageSize);
    Task<TemplateModel> GetTemplateByIdAsync(int id);
    Task<TemplateModel> CreateTemplateAsync(CreateTemplateRequest request, string createdBy);
    Task DeleteTemplateAsync(int id);
    Task<string> GetTemplatePreviewUrlAsync(int id);
    Task<List<string>> GetInvoiceTemplateNamesAsync();
    Task<List<string>> GetQuoteTemplateNamesAsync();
}
