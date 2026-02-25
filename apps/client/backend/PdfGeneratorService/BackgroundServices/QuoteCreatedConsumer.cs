using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using PdfGeneratorService.Services.Generation;
using PdfGeneratorService.Services.Storage;
using System.Text.Json;

namespace PdfGeneratorService.BackgroundServices;

public class QuoteCreatedConsumer : BackgroundService
{
    private readonly ILogger<QuoteCreatedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly string _topic = "quote-created";

    public QuoteCreatedConsumer(
        ILogger<QuoteCreatedConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quote Created Consumer starting...");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
            GroupId = "pdf-generator-service-quotes",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            SocketTimeoutMs = 5000,
            SessionTimeoutMs = 10000
        };

        IConsumer<string, string>? consumer = null;

        try
        {
            consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    if (e.IsFatal)
                    {
                        _logger.LogError("Fatal Kafka error: {Reason}", e.Reason);
                    }
                    else
                    {
                        _logger.LogWarning("Kafka error (non-fatal): {Reason}", e.Reason);
                    }
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

                        await ProcessQuoteCreatedEventAsync(consumeResult.Message.Value, stoppingToken);

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
                    _logger.LogError(ex, "Error processing quote created event");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Quote Created Consumer is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Quote Created Consumer");
        }
        finally
        {
            if (consumer != null)
            {
                consumer.Close();
                consumer.Dispose();
            }
            _logger.LogInformation("Quote Created Consumer stopped");
        }
    }

    private async Task ProcessQuoteCreatedEventAsync(string messageValue, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<QuoteCreatedEvent>(messageValue);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", messageValue);
                return;
            }

            _logger.LogInformation("Processing quote created event for QuoteId: {QuoteId}", message.QuoteId);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfGenerationService>();
            var minioService = scope.ServiceProvider.GetRequiredService<IMinioStorageService>();

            // Fetch quote with items, client, and organization (with address) from database
            var quote = await dbContext.Quotes
                .Include(q => q.Items)
                .Include(q => q.Client)
                .Include(q => q.Organization)
                    .ThenInclude(o => o!.Address)
                .FirstOrDefaultAsync(q => q.Id == message.QuoteId, cancellationToken);

            if (quote == null)
            {
                _logger.LogWarning("Quote {QuoteId} not found in database", message.QuoteId);
                return;
            }

            // Generate PDF with specified template
            var pdfBytes = await pdfService.GeneratePdfFromQuoteAsync(quote, quote.TemplateId);

            // Upload to MinIO
            var storageKey = await minioService.UploadQuotePdfAsync(quote.Id, pdfBytes);

            // Update quote with PDF storage key
            quote.PdfStorageKey = storageKey;

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully processed quote {QuoteId}. PDF stored at: {StorageKey}",
                quote.Id, storageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessQuoteCreatedEventAsync");
            throw;
        }
    }

    private class QuoteCreatedEvent
    {
        public int QuoteId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
