using InvoiceTrackerApi.Exceptions;

namespace InvoiceTrackerApi.Services.PdfStorage;

/// <summary>
/// Service implementation for PDF storage operations
/// </summary>
public class PdfStorageService : IPdfStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PdfStorageService> _logger;

    public PdfStorageService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<PdfStorageService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetPresignedUrlAsync(string storageKey)
    {
        if (string.IsNullOrEmpty(storageKey))
        {
            throw new ArgumentException("Storage key cannot be null or empty", nameof(storageKey));
        }

        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{pdfServiceUrl}/api/Pdf/presigned-url?storageKey={Uri.EscapeDataString(storageKey)}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return result?["url"] ?? throw new BusinessRuleException("Invalid response from PDF Generator Service");
            }

            _logger.LogWarning("Failed to get presigned URL from PDF Generator Service. Status: {Status}", response.StatusCode);
            throw new BusinessRuleException("Failed to generate PDF URL");
        }
        catch (Exception ex) when (ex is not BusinessRuleException and not ArgumentException)
        {
            _logger.LogError(ex, "Error getting presigned URL for storage key: {StorageKey}", storageKey);
            throw new BusinessRuleException("Error generating PDF URL");
        }
    }
}
