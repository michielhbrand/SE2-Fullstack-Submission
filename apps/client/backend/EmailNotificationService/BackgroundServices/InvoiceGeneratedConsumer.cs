using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Shared.Database.Data;
using EmailNotificationService.Services;
using System.Text.Json;

namespace EmailNotificationService.BackgroundServices;

/// <summary>
/// Kafka consumer for "invoice-generated" topic.
/// When an invoice is generated (e.g. from quote conversion),
/// this consumer sends a notification email to the client.
/// </summary>
public class InvoiceGeneratedConsumer : BackgroundService
{
    private readonly ILogger<InvoiceGeneratedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly string _topic = "invoice-generated";

    public InvoiceGeneratedConsumer(
        ILogger<InvoiceGeneratedConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Generated Consumer starting...");

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

                        await ProcessInvoiceGeneratedAsync(consumeResult.Message.Value, stoppingToken);

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
                    _logger.LogError(ex, "Error processing invoice generated event");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Invoice Generated Consumer is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Invoice Generated Consumer");
        }
        finally
        {
            if (consumer != null)
            {
                consumer.Close();
                consumer.Dispose();
            }
            _logger.LogInformation("Invoice Generated Consumer stopped");
        }
    }

    private async Task ProcessInvoiceGeneratedAsync(string messageValue, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<InvoiceGeneratedEvent>(messageValue);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", messageValue);
                return;
            }

            _logger.LogInformation(
                "Processing invoice generated event for InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}",
                message.InvoiceId, message.WorkflowId);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Fetch the invoice with client info
            var invoice = await dbContext.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == message.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found in database", message.InvoiceId);
                return;
            }

            if (invoice.Client == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} has no associated client", message.InvoiceId);
                return;
            }

            var clientEmail = invoice.Client.Email;
            var clientName = invoice.Client.Name;

            if (string.IsNullOrEmpty(clientEmail))
            {
                _logger.LogWarning("Client {ClientId} has no email address", invoice.ClientId);
                return;
            }

            // Fetch the invoice PDF from MinIO if available
            byte[]? pdfBytes = null;
            if (!string.IsNullOrEmpty(invoice.PdfStorageKey))
            {
                try
                {
                    pdfBytes = await FetchPdfFromMinioAsync(invoice.PdfStorageKey);
                    _logger.LogInformation("Fetched PDF for Invoice {InvoiceId} ({Size} bytes)", message.InvoiceId, pdfBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch PDF for Invoice {InvoiceId}. Email will be sent without attachment.", message.InvoiceId);
                }
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} has no PDF storage key. Email will be sent without attachment.", message.InvoiceId);
            }

            // Send the email with PDF attachment
            await emailService.SendInvoiceGeneratedEmailAsync(
                clientEmail, clientName, message.InvoiceId, message.WorkflowId, pdfBytes);

            _logger.LogInformation(
                "Invoice payment email sent successfully for InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}",
                message.InvoiceId, message.WorkflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessInvoiceGeneratedAsync");
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

    private class InvoiceGeneratedEvent
    {
        public int InvoiceId { get; set; }
        public int WorkflowId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
