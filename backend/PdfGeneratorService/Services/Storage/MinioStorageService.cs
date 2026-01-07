using Minio;
using Minio.DataModel.Args;

namespace PdfGeneratorService.Services.Storage;

public class MinioStorageService : IMinioStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly string _bucketName = "invoices";

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

    public async Task<string> UploadPdfAsync(int invoiceId, byte[] pdfBytes)
    {
        try
        {
            _logger.LogInformation("Starting PDF upload for Invoice {InvoiceId}, Size: {Size} bytes", invoiceId, pdfBytes.Length);

            // Ensure bucket exists
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_bucketName);
            
            bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
            
            if (!bucketExists)
            {
                _logger.LogInformation("Bucket {BucketName} does not exist, creating it", _bucketName);
                
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
                
                _logger.LogInformation("Created bucket: {BucketName}", _bucketName);
            }

            // Generate unique object name
            var objectName = $"invoice-{invoiceId}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

            // Upload PDF using MemoryStream
            using var stream = new MemoryStream(pdfBytes);
            
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("application/pdf");

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("Successfully uploaded PDF to MinIO. Bucket: {Bucket}, Object: {Object}", _bucketName, objectName);

            // Return the storage key
            return $"{_bucketName}/{objectName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF to MinIO for Invoice {InvoiceId}", invoiceId);
            throw;
        }
    }
}
