using Shared.Database.Models;
using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Services.Workflow;

/// <summary>
/// Dispatches Kafka events in response to workflow state changes.
/// Owns the event-type → topic mapping for approval, payment, and overdue notifications.
/// </summary>
public class WorkflowEventDispatcher : IWorkflowEventDispatcher
{
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<WorkflowEventDispatcher> _logger;

    public WorkflowEventDispatcher(IKafkaProducerService kafkaProducer, ILogger<WorkflowEventDispatcher> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task DispatchAsync(WorkflowEvent workflowEvent, WorkflowModel workflow, CancellationToken ct = default)
    {
        var eventType  = workflowEvent.EventType;
        var workflowId = workflow.Id;

        if (eventType == WorkflowEventType.SentForApproval || eventType == WorkflowEventType.ResentForApproval)
        {
            try
            {
                if (workflow.QuoteId.HasValue)
                {
                    await _kafkaProducer.PublishQuoteApprovalRequestedEventAsync(workflow.QuoteId.Value, workflowId);
                    _logger.LogInformation(
                        "Quote approval requested event published for QuoteId: {QuoteId}, WorkflowId: {WorkflowId}",
                        workflow.QuoteId.Value, workflowId);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot publish quote approval event — workflow {WorkflowId} has no linked QuoteId", workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish quote approval requested event for WorkflowId: {WorkflowId}. " +
                    "Workflow status updated but email notification not triggered.", workflowId);
            }
        }

        if (eventType == WorkflowEventType.SentForPayment || eventType == WorkflowEventType.ResentForPayment)
        {
            try
            {
                if (workflow.InvoiceId.HasValue)
                {
                    await _kafkaProducer.PublishInvoiceGeneratedEventAsync(workflow.InvoiceId.Value, workflowId);
                    _logger.LogInformation(
                        "Invoice generated event published for InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}",
                        workflow.InvoiceId.Value, workflowId);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot publish invoice generated event — workflow {WorkflowId} has no linked InvoiceId", workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish invoice generated event for WorkflowId: {WorkflowId}. " +
                    "Workflow status updated but email notification not triggered.", workflowId);
            }
        }

        if (eventType == WorkflowEventType.OverdueReminderSent)
        {
            try
            {
                if (workflow.InvoiceId.HasValue)
                {
                    await _kafkaProducer.PublishInvoiceOverdueEventAsync(workflow.InvoiceId.Value, workflowId);
                    _logger.LogInformation(
                        "Overdue reminder event published for InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}",
                        workflow.InvoiceId.Value, workflowId);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot publish overdue reminder event — workflow {WorkflowId} has no linked InvoiceId", workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish overdue reminder event for WorkflowId: {WorkflowId}. " +
                    "Workflow event recorded but email notification not triggered.", workflowId);
            }
        }
    }
}
