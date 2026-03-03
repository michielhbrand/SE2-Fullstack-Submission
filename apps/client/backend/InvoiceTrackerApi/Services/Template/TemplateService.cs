using InvoiceTrackerApi.DTOs.Template.Requests;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Template.Responses;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Template;
using Shared.Database.Models;
using TemplateModel = Shared.Database.Models.Template;

namespace InvoiceTrackerApi.Services.Template;

/// <summary>
/// Service implementation for Template business logic
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ITemplateRepository templateRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<TemplateService> logger)
    {
        _templateRepository = templateRepository;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PaginatedResponse<TemplateResponse>> GetTemplatesAsync(int organizationId, int page, int pageSize, string? search = null, TemplateType? type = null)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var templates = await _templateRepository.GetAllByOrganizationAsync(organizationId, page, pageSize, search, type);
        var totalCount = await _templateRepository.GetTotalCountByOrganizationAsync(organizationId, search, type);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<TemplateResponse>
        {
            Data = templates.Select(t => t.ToDto()).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<TemplateResponse> GetTemplateByIdAsync(int id)
    {
        var template = await _templateRepository.GetByIdAsync(id);

        if (template == null)
        {
            throw new NotFoundException("Template", id);
        }

        return template.ToDto();
    }

    public async Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request, string createdBy, int organizationId)
    {
        // Business rule validation: Check for duplicate template name and version within the org
        var existingTemplate = await _templateRepository.GetByNameAndVersionAndOrgAsync(request.Name, request.Version, organizationId);
        if (existingTemplate != null)
        {
            throw new DuplicateEntityException($"A template with name '{request.Name}' and version {request.Version} already exists in this organization");
        }

        // Determine the bucket based on template type
        var bucketName = request.Type == TemplateType.Quote ? "quote-templates" : "templates";

        // Upload the template HTML to PdfGeneratorService
        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            var uploadResponse = await httpClient.PostAsJsonAsync(
                $"{pdfServiceUrl}/api/Template",
                new { name = request.Name, content = request.Content });

            if (!uploadResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to upload template to PdfGeneratorService. Status: {Status}", uploadResponse.StatusCode);
                throw new BusinessRuleException("Failed to upload template to storage");
            }

            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadTemplateResponse>();

            // Create the template record first to get the ID
            var template = new TemplateModel
            {
                CreatedBy = createdBy,
                Created = _timeProvider.GetUtcNow().UtcDateTime,
                Version = request.Version,
                Name = request.Name,
                Type = request.Type,
                OrganizationId = organizationId,
                StorageKey = $"{bucketName}/{uploadResult?.Name}" // Temporary, uses the uploaded name
            };

            var createdTemplate = await _templateRepository.AddAsync(template);

            _logger.LogInformation("Template created: {TemplateName} v{Version} (Type: {Type}, Org: {OrgId}) by user {UserId}",
                template.Name, template.Version, template.Type, organizationId, createdBy);

            return createdTemplate.ToDto();
        }
        catch (Exception ex) when (ex is not BusinessRuleException && ex is not DuplicateEntityException)
        {
            _logger.LogError(ex, "Error creating template");
            throw new BusinessRuleException("Error creating template");
        }
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _templateRepository.GetByIdAsync(id);

        if (template == null)
        {
            throw new NotFoundException("Template", id);
        }

        await _templateRepository.DeleteAsync(template);

        _logger.LogInformation("Template deleted: {TemplateId}", id);
    }

    public async Task<string> GetTemplatePreviewUrlAsync(int id)
    {
        var template = await _templateRepository.GetByIdAsync(id);

        if (template == null)
        {
            throw new NotFoundException("Template", id);
        }

        // Use the template ID for the preview endpoint on PdfGeneratorService
        var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
        var previewUrl = $"{pdfServiceUrl}/api/Template/{template.Id}/preview";

        return previewUrl;
    }

    public async Task<List<TemplateResponse>> GetTemplatesByTypeAsync(int organizationId, TemplateType type)
    {
        var templates = await _templateRepository.GetByOrganizationAndTypeAsync(organizationId, type);
        return templates.Select(t => t.ToDto()).ToList();
    }
}
