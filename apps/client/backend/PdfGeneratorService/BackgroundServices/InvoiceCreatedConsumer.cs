using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;
using PdfGeneratorService.Services.Generation;
using PdfGeneratorService.Services.Storage;
using System.Text.Json;

namespace PdfGeneratorService.BackgroundServices;

public class InvoiceCreatedConsumer : BackgroundService
{
    private readonly ILogger<InvoiceCreatedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly string _topic = "invoice-created";

    public InvoiceCreatedConsumer(
        ILogger<InvoiceCreatedConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Created Consumer starting...");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
            GroupId = "pdf-generator-service",
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

                        await ProcessInvoiceCreatedEventAsync(consumeResult.Message.Value, stoppingToken);

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
                    _logger.LogError(ex, "Error processing invoice created event");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Invoice Created Consumer is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Invoice Created Consumer");
        }
        finally
        {
            if (consumer != null)
            {
                consumer.Close();
                consumer.Dispose();
            }
            _logger.LogInformation("Invoice Created Consumer stopped");
        }
    }

    private async Task ProcessInvoiceCreatedEventAsync(string messageValue, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<InvoiceCreatedEvent>(messageValue);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Message}", messageValue);
                return;
            }

            _logger.LogInformation("Processing invoice created event for InvoiceId: {InvoiceId}", message.InvoiceId);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfGenerationService>();
            var minioService = scope.ServiceProvider.GetRequiredService<IMinioStorageService>();

            // Fetch invoice with items, client, and organization (with address) from database
            var invoice = await dbContext.Invoices
                .Include(i => i.Items)
                .Include(i => i.Client)
                .Include(i => i.Organization)
                    .ThenInclude(o => o!.Address)
                .FirstOrDefaultAsync(i => i.Id == message.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found in database", message.InvoiceId);
                return;
            }

            // Fetch active bank accounts for the organization
            var bankAccounts = await dbContext.BankAccounts
                .Where(b => b.OrganizationId == invoice.OrganizationId && b.Active)
                .ToListAsync(cancellationToken);

            // Generate PDF with specified template
            var pdfBytes = await pdfService.GeneratePdfFromInvoiceAsync(invoice, bankAccounts, invoice.TemplateId);

            // Upload to MinIO
            var storageKey = await minioService.UploadPdfAsync(invoice.Id, pdfBytes);

            // Update invoice with PDF storage key
            invoice.PdfStorageKey = storageKey;

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully processed invoice {InvoiceId}. PDF stored at: {StorageKey}",
                invoice.Id, storageKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessInvoiceCreatedEventAsync");
            throw;
        }
    }

    private class InvoiceCreatedEvent
    {
        public int InvoiceId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
