using PdfGeneratorService.Services.Storage;

namespace PdfGeneratorService.BackgroundServices;

public class MinioInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MinioInitializationService> _logger;

    public MinioInitializationService(
        IServiceProvider serviceProvider,
        ILogger<MinioInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000, cancellationToken); // Wait for app to fully start
                
                using var scope = _serviceProvider.CreateScope();
                var storageService = scope.ServiceProvider.GetRequiredService<IMinioStorageService>();
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                _logger.LogInformation("Initializing MinIO buckets...");
                await storageService.EnsureBucketsExistAsync();
                _logger.LogInformation("MinIO buckets initialized successfully");

                // Upload default invoice template if it doesn't exist
                var invoiceTemplates = await storageService.ListTemplatesAsync();
                if (!invoiceTemplates.Contains("InvoiceTemplate.html"))
                {
                    _logger.LogInformation("Uploading default invoice template...");
                    var templatePath = Path.Combine(env.ContentRootPath, "Templates", "InvoiceTemplate.html");
                    var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
                    await storageService.UploadTemplateAsync("InvoiceTemplate.html", templateContent);
                    _logger.LogInformation("Default invoice template uploaded successfully");
                }
                else
                {
                    _logger.LogInformation("Default invoice template already exists in MinIO");
                }

                // Upload default quote template if it doesn't exist
                var quoteTemplates = await storageService.ListQuoteTemplatesAsync();
                if (!quoteTemplates.Contains("QuoteTemplate.html"))
                {
                    _logger.LogInformation("Uploading default quote template...");
                    var quoteTemplatePath = Path.Combine(env.ContentRootPath, "Templates", "QuoteTemplate.html");
                    var quoteTemplateContent = await File.ReadAllTextAsync(quoteTemplatePath, cancellationToken);
                    await storageService.UploadQuoteTemplateAsync("QuoteTemplate.html", quoteTemplateContent);
                    _logger.LogInformation("Default quote template uploaded successfully");
                }
                else
                {
                    _logger.LogInformation("Default quote template already exists in MinIO");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MinIO initialization");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
