using InvoiceTrackerApi.DTOs.Template.Requests;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Template.Responses;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Template;
using TemplateModel = InvoiceTrackerApi.Models.Template;

namespace InvoiceTrackerApi.Services.Template;

/// <summary>
/// Service implementation for Template business logic
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ITemplateRepository templateRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TemplateService> logger)
    {
        _templateRepository = templateRepository;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaginatedResponse<TemplateResponse>> GetTemplatesAsync(int page, int pageSize)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var templates = await _templateRepository.GetAllAsync(page, pageSize);
        var totalCount = await _templateRepository.GetTotalCountAsync();
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

    public async Task<TemplateResponse> CreateTemplateAsync(CreateTemplateRequest request, string createdBy)
    {
        // Business rule validation: Check for duplicate template name and version
        var existingTemplate = await _templateRepository.GetByNameAndVersionAsync(request.Name, request.Version);
        if (existingTemplate != null)
        {
            throw new DuplicateEntityException($"A template with name '{request.Name}' and version {request.Version} already exists");
        }

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
            var storageKey = $"templates/{uploadResult?.Name}";

            // Create the template record
            var template = new TemplateModel
            {
                CreatedBy = createdBy,
                Created = DateTime.UtcNow,
                Version = request.Version,
                Name = request.Name,
                StorageKey = storageKey
            };

            var createdTemplate = await _templateRepository.AddAsync(template);

            _logger.LogInformation("Template created: {TemplateName} v{Version} by user {UserId}",
                template.Name, template.Version, createdBy);

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

        // Extract template name from storage key
        var templateName = template.StorageKey.Replace("templates/", "");
        var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
        var previewUrl = $"{pdfServiceUrl}/api/Template/{Uri.EscapeDataString(templateName)}/preview";

        return previewUrl;
    }

    public async Task<List<string>> GetInvoiceTemplateNamesAsync()
    {
        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{pdfServiceUrl}/api/Template");

            if (response.IsSuccessStatusCode)
            {
                var templates = await response.Content.ReadFromJsonAsync<List<string>>();
                return templates ?? new List<string>();
            }

            _logger.LogWarning("Failed to fetch invoice templates from PDF Generator Service. Status: {Status}", response.StatusCode);
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoice templates from PDF Generator Service");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetQuoteTemplateNamesAsync()
    {
        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{pdfServiceUrl}/api/Template/quote-templates");

            if (response.IsSuccessStatusCode)
            {
                var templates = await response.Content.ReadFromJsonAsync<List<string>>();
                return templates ?? new List<string>();
            }

            _logger.LogWarning("Failed to fetch quote templates from PDF Generator Service. Status: {Status}", response.StatusCode);
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quote templates from PDF Generator Service");
            return new List<string>();
        }
    }
}
