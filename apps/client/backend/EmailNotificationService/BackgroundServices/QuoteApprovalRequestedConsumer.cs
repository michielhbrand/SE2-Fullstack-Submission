using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Shared.Database.Data;
using EmailNotificationService.Services;
using System.Text.Json;

namespace EmailNotificationService.BackgroundServices;

/// <summary>
/// Kafka consumer for "quote-approval-requested" topic.
/// When a quote is sent for approval, this consumer sends an email
/// to the client with Accept/Reject buttons.
/// </summary>
public class QuoteApprovalRequestedConsumer : BackgroundService
{
    private readonly ILogger<QuoteApprovalRequestedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly string _topic = "quote-approval-requested";

    public QuoteApprovalRequestedConsumer(
        ILogger<QuoteApprovalRequestedConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quote Approval Requested Consumer starting...");

        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
            GroupId = "email-notification-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            SocketTimeoutMs = 10000,
            SessionTimeoutMs = 30000,
            ReconnectBackoffMs = 1000,
            ReconnectBackoffMaxMs = 30000,
            LogConnectionClose = false
        };

        IConsumer<string, string>? consumer = null;

        try
        {
            consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    if (e.IsFatal)
                        _logger.LogError("Fatal Kafka error: {Reason}", e.Reason);
                    else
                        _logger.LogDebug("Kafka error (non-fatal): {Reason}", e.Reason);
                })
                .SetLogHandler((_, log) =>
                {
                    // Route librdkafka native logs through .NET logging at Debug level
                    _logger.LogDebug("Kafka [{Level}] {Facility}: {Message}", log.Level, log.Facility, log.Message);
                })
                .Build();

            _logger.LogInformation("Subscribing to topic: {Topic}", _topic);
            consumer.Subscribe(_topic);
            _logger.LogInformation("Kafka consumer ready. Waiting for messages on topic: {Topic}", _topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult?.Message?.Value != null)
                    {
                        _logger.LogInformation(
                            "Received message from Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

                        await ProcessQuoteApprovalRequestedAsync(consumeResult.Message.Value, stoppingToken);

                        consumer.Commit(consumeResult);
                        _logger.LogInformation("Message committed successfully");
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogDebug("Topic {Topic} not yet created. Waiting...", _topic);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.Local_AllBrokersDown)
                {
                    _logger.LogWarning("Kafka brokers are down. Retrying in 10 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing quote approval requested event");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Quote Approval Requested Consumer is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Quote Approval Requested Consumer");
        }
        finally
        {
            if (consumer != null)
            {
                consumer.Close();
                consumer.Dispose();
            }
            _logger.LogInformation("Quote Approval Requested Consumer stopped");
        }
    }

    private async Task ProcessQuoteApprovalRequestedAsync(string messageValue, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<QuoteApprovalRequestedEvent>(messageValue);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", messageValue);
                return;
            }

            _logger.LogInformation(
                "Processing quote approval requested event for QuoteId: {QuoteId}, WorkflowId: {WorkflowId}",
                message.QuoteId, message.WorkflowId);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            // Fetch the quote with client info
            var quote = await dbContext.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.Id == message.QuoteId, cancellationToken);

            if (quote == null)
            {
                _logger.LogWarning("Quote {QuoteId} not found in database", message.QuoteId);
                return;
            }

            if (quote.Client == null)
            {
                _logger.LogWarning("Quote {QuoteId} has no associated client", message.QuoteId);
                return;
            }

            var clientEmail = quote.Client.Email;
            var clientName = quote.Client.Name;

            if (string.IsNullOrEmpty(clientEmail))
            {
                _logger.LogWarning("Client {ClientId} has no email address", quote.ClientId);
                return;
            }

            // Fetch the quote PDF from MinIO if available
            byte[]? pdfBytes = null;
            if (!string.IsNullOrEmpty(quote.PdfStorageKey))
            {
                try
                {
                    pdfBytes = await FetchPdfFromMinioAsync(quote.PdfStorageKey);
                    _logger.LogInformation("Fetched PDF for Quote {QuoteId} ({Size} bytes)", message.QuoteId, pdfBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch PDF for Quote {QuoteId}. Email will be sent without attachment.", message.QuoteId);
                }
            }
            else
            {
                _logger.LogWarning("Quote {QuoteId} has no PDF storage key. Email will be sent without attachment.", message.QuoteId);
            }

            // Generate approval/rejection tokens
            var approveToken = tokenService.GenerateToken(message.WorkflowId, message.QuoteId, "approve");
            var rejectToken = tokenService.GenerateToken(message.WorkflowId, message.QuoteId, "reject");

            // Send the email with PDF attachment
            await emailService.SendQuoteApprovalEmailAsync(
                clientEmail, clientName, message.QuoteId, message.WorkflowId,
                approveToken, rejectToken, pdfBytes);

            _logger.LogInformation(
                "Quote approval email sent successfully for QuoteId: {QuoteId}, WorkflowId: {WorkflowId}",
                message.QuoteId, message.WorkflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessQuoteApprovalRequestedAsync");
            throw;
        }
    }

    private async Task<byte[]> FetchPdfFromMinioAsync(string storageKey)
    {
        var endpoint = _configuration["MinIO:Endpoint"] ?? "localhost:9002";
        var accessKey = _configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = _configuration["MinIO:SecretKey"] ?? "minioadmin";

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 5
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(2),
            DefaultRequestVersion = new Version(1, 1)
        };

        var minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .WithHttpClient(httpClient)
            .Build();

        // Parse storage key (format: "bucket/objectName")
        var parts = storageKey.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid storage key format: {storageKey}. Expected 'bucket/objectName'");
        }

        var bucketName = parts[0];
        var objectName = parts[1];

        var memoryStream = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(memoryStream);
            });

        await minioClient.GetObjectAsync(getObjectArgs);

        return memoryStream.ToArray();
    }

    private class QuoteApprovalRequestedEvent
    {
        public int QuoteId { get; set; }
        public int WorkflowId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
