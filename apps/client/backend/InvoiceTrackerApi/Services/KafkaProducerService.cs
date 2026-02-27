using Confluent.Kafka;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace InvoiceTrackerApi.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _invoiceTopic = "invoice-created";
    private readonly string _quoteTopic = "quote-created";
    private readonly string _quoteApprovalTopic = "quote-approval-requested";
    private readonly string _invoiceGeneratedTopic = "invoice-generated";
    private readonly string _invoiceOverdueTopic = "invoice-overdue";

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9093"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishInvoiceCreatedEventAsync(int invoiceId)
    {
        try
        {
            var message = new
            {
                InvoiceId = invoiceId,
                Timestamp = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = invoiceId.ToString(),
                Value = messageJson
            };
            InjectTraceContext(kafkaMessage);
            var result = await _producer.ProduceAsync(_invoiceTopic, kafkaMessage);

            _logger.LogInformation(
                "Invoice created event published to Kafka. InvoiceId: {InvoiceId}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                invoiceId, _invoiceTopic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing invoice created event for InvoiceId: {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task PublishQuoteCreatedEventAsync(int quoteId)
    {
        try
        {
            var message = new
            {
                QuoteId = quoteId,
                Timestamp = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = quoteId.ToString(),
                Value = messageJson
            };
            InjectTraceContext(kafkaMessage);
            var result = await _producer.ProduceAsync(_quoteTopic, kafkaMessage);

            _logger.LogInformation(
                "Quote created event published to Kafka. QuoteId: {QuoteId}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                quoteId, _quoteTopic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing quote created event for QuoteId: {QuoteId}", quoteId);
            throw;
        }
    }

    public async Task PublishQuoteApprovalRequestedEventAsync(int quoteId, int workflowId)
    {
        try
        {
            var message = new
            {
                QuoteId = quoteId,
                WorkflowId = workflowId,
                Timestamp = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = quoteId.ToString(),
                Value = messageJson
            };
            InjectTraceContext(kafkaMessage);
            var result = await _producer.ProduceAsync(_quoteApprovalTopic, kafkaMessage);

            _logger.LogInformation(
                "Quote approval requested event published to Kafka. QuoteId: {QuoteId}, WorkflowId: {WorkflowId}, Topic: {Topic}",
                quoteId, workflowId, _quoteApprovalTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing quote approval requested event for QuoteId: {QuoteId}", quoteId);
            throw;
        }
    }

    public async Task PublishInvoiceGeneratedEventAsync(int invoiceId, int workflowId)
    {
        try
        {
            var message = new
            {
                InvoiceId = invoiceId,
                WorkflowId = workflowId,
                Timestamp = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = invoiceId.ToString(),
                Value = messageJson
            };
            InjectTraceContext(kafkaMessage);
            var result = await _producer.ProduceAsync(_invoiceGeneratedTopic, kafkaMessage);

            _logger.LogInformation(
                "Invoice generated event published to Kafka. InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}, Topic: {Topic}",
                invoiceId, workflowId, _invoiceGeneratedTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing invoice generated event for InvoiceId: {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task PublishInvoiceOverdueEventAsync(int invoiceId, int workflowId)
    {
        try
        {
            var message = new
            {
                InvoiceId = invoiceId,
                WorkflowId = workflowId,
                Timestamp = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(message);

            var kafkaMessage = new Message<string, string>
            {
                Key = invoiceId.ToString(),
                Value = messageJson
            };
            InjectTraceContext(kafkaMessage);
            var result = await _producer.ProduceAsync(_invoiceOverdueTopic, kafkaMessage);

            _logger.LogInformation(
                "Invoice overdue event published to Kafka. InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}, Topic: {Topic}",
                invoiceId, workflowId, _invoiceOverdueTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing invoice overdue event for InvoiceId: {InvoiceId}", invoiceId);
            throw;
        }
    }

    private static void InjectTraceContext(Message<string, string> message)
    {
        var activity = Activity.Current;
        if (activity == null) return;
        message.Headers ??= new Headers();
        var traceparent = $"00-{activity.TraceId}-{activity.SpanId}-" +
            $"{(activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00")}";
        message.Headers.Add("traceparent", Encoding.UTF8.GetBytes(traceparent));
        if (!string.IsNullOrEmpty(activity.TraceStateString))
            message.Headers.Add("tracestate", Encoding.UTF8.GetBytes(activity.TraceStateString));
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
