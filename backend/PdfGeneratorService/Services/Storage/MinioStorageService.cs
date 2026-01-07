using Minio;
using Minio.DataModel.Args;
using System.Text;

namespace PdfGeneratorService.Services.Storage;

public class MinioStorageService : IMinioStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly string _invoicesBucketName = "invoices";
    private readonly string _templatesBucketName = "templates";

    public MinioStorageService(IConfiguration configuration, ILogger<MinioStorageService> logger)
    {
        _logger = logger;

        var endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9002";
        var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";

        _logger.LogInformation("Initializing MinIO client with endpoint: {Endpoint}", endpoint);

        // Configure HttpClient with HTTP/1.1 for compatibility with MinIO
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10
        };
        
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(5),
            DefaultRequestVersion = new Version(1, 1)
        };

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .WithHttpClient(httpClient)
            .Build();
        
        _logger.LogInformation("MinIO client initialized successfully");
    }

    public async Task EnsureBucketsExistAsync()
    {
        try
        {
            await EnsureBucketExistsAsync(_invoicesBucketName);
            await EnsureBucketExistsAsync(_templatesBucketName);
            _logger.LogInformation("All required buckets are ready");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring buckets exist");
            throw;
        }
    }

    private async Task EnsureBucketExistsAsync(string bucketName)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        
        bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
        
        if (!bucketExists)
        {
            _logger.LogInformation("Bucket {BucketName} does not exist, creating it", bucketName);
            
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs);
            
            _logger.LogInformation("Created bucket: {BucketName}", bucketName);
        }
    }

    public async Task<string> UploadPdfAsync(int invoiceId, byte[] pdfBytes)
    {
        try
        {
            _logger.LogInformation("Starting PDF upload for Invoice {InvoiceId}, Size: {Size} bytes", invoiceId, pdfBytes.Length);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(_invoicesBucketName);

            // Generate unique object name
            var objectName = $"invoice-{invoiceId}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

            // Upload PDF using MemoryStream
            using var stream = new MemoryStream(pdfBytes);
            
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_invoicesBucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("application/pdf");

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("Successfully uploaded PDF to MinIO. Bucket: {Bucket}, Object: {Object}", _invoicesBucketName, objectName);

            // Return the storage key
            return $"{_invoicesBucketName}/{objectName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF to MinIO for Invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<string> UploadTemplateAsync(string templateName, string htmlContent)
    {
        try
        {
            _logger.LogInformation("Starting template upload: {TemplateName}", templateName);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(_templatesBucketName);

            // Ensure template name ends with .html
            if (!templateName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                templateName += ".html";
            }

            // Upload template using MemoryStream
            var bytes = Encoding.UTF8.GetBytes(htmlContent);
            using var stream = new MemoryStream(bytes);
            
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_templatesBucketName)
                .WithObject(templateName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("text/html");

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("Successfully uploaded template to MinIO. Bucket: {Bucket}, Object: {Object}", _templatesBucketName, templateName);

            return templateName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template {TemplateName} to MinIO", templateName);
            throw;
        }
    }

    public async Task<List<string>> ListTemplatesAsync()
    {
        try
        {
            _logger.LogInformation("Listing templates from bucket: {Bucket}", _templatesBucketName);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(_templatesBucketName);

            var templates = new List<string>();
            var listArgs = new ListObjectsArgs()
                .WithBucket(_templatesBucketName)
                .WithRecursive(true);

            var observable = _minioClient.ListObjectsEnumAsync(listArgs);
            
            await foreach (var item in observable)
            {
                if (item.Key.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    templates.Add(item.Key);
                }
            }

            _logger.LogInformation("Found {Count} templates in bucket", templates.Count);
            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing templates from MinIO");
            throw;
        }
    }

    public async Task<string> GetTemplateAsync(string templateName)
    {
        try
        {
            _logger.LogInformation("Retrieving template: {TemplateName}", templateName);

            // Ensure template name ends with .html
            if (!templateName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                templateName += ".html";
            }

            using var memoryStream = new MemoryStream();
            
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_templatesBucketName)
                .WithObject(templateName)
                .WithCallbackStream(async (stream) =>
                {
                    await stream.CopyToAsync(memoryStream);
                });

            await _minioClient.GetObjectAsync(getObjectArgs);

            var htmlContent = Encoding.UTF8.GetString(memoryStream.ToArray());
            
            _logger.LogInformation("Successfully retrieved template: {TemplateName}, Size: {Size} bytes",
                templateName, memoryStream.Length);

            return htmlContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateName} from MinIO", templateName);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string storageKey, int expiryInSeconds = 3600)
    {
        try
        {
            _logger.LogInformation("Generating presigned URL for storage key: {StorageKey}", storageKey);

            // Parse the storage key (format: "bucket/objectName")
            var parts = storageKey.Split('/', 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid storage key format: {storageKey}. Expected format: 'bucket/objectName'");
            }

            var bucketName = parts[0];
            var objectName = parts[1];

            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiryInSeconds);

            var presignedUrl = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogInformation("Successfully generated presigned URL for {StorageKey}", storageKey);
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for storage key: {StorageKey}", storageKey);
            throw;
        }
    }
}
