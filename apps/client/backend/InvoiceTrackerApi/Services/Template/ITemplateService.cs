using InvoiceTrackerApi.DTOs.Template.Requests;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Template.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Services.Template;

/// <summary>
/// Service interface for Template business logic
/// </summary>
public interface ITemplateService
{
    Task<PaginatedResponse<TemplateResponse>> GetTemplatesAsync(int organizationId, int page, int pageSize, string? search = null, TemplateType? type = null);
    Task<TemplateResponse> GetTemplateByIdAsync(int id);
    Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request, string createdBy, int organizationId);
    Task DeleteTemplateAsync(int id);
    Task<string> GetTemplatePreviewUrlAsync(int id);
    Task<List<TemplateResponse>> GetTemplatesByTypeAsync(int organizationId, TemplateType type);
}
