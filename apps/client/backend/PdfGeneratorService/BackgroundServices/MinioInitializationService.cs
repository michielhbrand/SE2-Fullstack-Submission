using PdfGeneratorService.Services.Storage;

namespace PdfGeneratorService.BackgroundServices;

public class MinioInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MinioInitializationService> _logger;

    /// <summary>
    /// Increment this version whenever templates are updated to force re-upload.
    /// </summary>
    private const int TemplateVersion = 2;

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

                // Always upload templates (force-overwrite to ensure latest version)
                _logger.LogInformation("Uploading default invoice template (version {Version})...", TemplateVersion);
                var invoiceTemplatePath = Path.Combine(env.ContentRootPath, "Templates", "InvoiceTemplate.html");
                var invoiceTemplateContent = await File.ReadAllTextAsync(invoiceTemplatePath, cancellationToken);
                await storageService.UploadTemplateAsync("InvoiceTemplate.html", invoiceTemplateContent);
                _logger.LogInformation("Default invoice template uploaded successfully (version {Version})", TemplateVersion);

                _logger.LogInformation("Uploading default quote template (version {Version})...", TemplateVersion);
                var quoteTemplatePath = Path.Combine(env.ContentRootPath, "Templates", "QuoteTemplate.html");
                var quoteTemplateContent = await File.ReadAllTextAsync(quoteTemplatePath, cancellationToken);
                await storageService.UploadQuoteTemplateAsync("QuoteTemplate.html", quoteTemplateContent);
                _logger.LogInformation("Default quote template uploaded successfully (version {Version})", TemplateVersion);
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
