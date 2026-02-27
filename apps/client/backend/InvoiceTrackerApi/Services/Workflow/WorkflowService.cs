using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Workflow.Responses;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Workflow;
using InvoiceTrackerApi.Services.Invoice;
using Microsoft.Extensions.DependencyInjection;
using Shared.Database.Models;
using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Services.Workflow;

/// <summary>
/// Service implementation for Workflow business logic with state machine validation
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowService> _logger;

    /// <summary>
    /// Valid status transitions: key = current status, value = set of allowed next statuses
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        [WorkflowStatus.Draft] = new HashSet<string>
        {
            WorkflowStatus.PendingApproval,
            WorkflowStatus.InvoiceCreated,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.PendingApproval] = new HashSet<string>
        {
            WorkflowStatus.PendingApproval, // resend for approval
            WorkflowStatus.Approved,
            WorkflowStatus.Rejected,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.Approved] = new HashSet<string>
        {
            WorkflowStatus.InvoiceCreated,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.Rejected] = new HashSet<string>
        {
            WorkflowStatus.PendingApproval, // re-send after modification
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.InvoiceCreated] = new HashSet<string>
        {
            WorkflowStatus.SentForPayment,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        [WorkflowStatus.SentForPayment] = new HashSet<string>
        {
            WorkflowStatus.SentForPayment, // resend for payment
            WorkflowStatus.Paid,
            WorkflowStatus.Cancelled,
            WorkflowStatus.Terminated
        },
        // Terminal states — no transitions allowed
        [WorkflowStatus.Paid] = new HashSet<string>(),
        [WorkflowStatus.Cancelled] = new HashSet<string>(),
        [WorkflowStatus.Terminated] = new HashSet<string>()
    };

    /// <summary>
    /// Maps event types to the resulting workflow status
    /// </summary>
    private static readonly Dictionary<string, string> EventToStatusMap = new()
    {
        [WorkflowEventType.QuoteCreated] = WorkflowStatus.Draft,
        [WorkflowEventType.SentForApproval] = WorkflowStatus.PendingApproval,
        [WorkflowEventType.Approved] = WorkflowStatus.Approved,
        [WorkflowEventType.Rejected] = WorkflowStatus.Rejected,
        [WorkflowEventType.QuoteModified] = WorkflowStatus.Rejected, // stays Rejected until re-sent
        [WorkflowEventType.ResentForApproval] = WorkflowStatus.PendingApproval,
        [WorkflowEventType.ConvertedToInvoice] = WorkflowStatus.InvoiceCreated,
        [WorkflowEventType.InvoiceCreated] = WorkflowStatus.InvoiceCreated,
        [WorkflowEventType.SentForPayment] = WorkflowStatus.SentForPayment,
        [WorkflowEventType.ResentForPayment] = WorkflowStatus.SentForPayment,
        [WorkflowEventType.OverdueReminderSent] = WorkflowStatus.SentForPayment, // status unchanged
        [WorkflowEventType.MarkedAsPaid] = WorkflowStatus.Paid,
        [WorkflowEventType.Cancelled] = WorkflowStatus.Cancelled,
        [WorkflowEventType.Terminated] = WorkflowStatus.Terminated
    };

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IKafkaProducerService kafkaProducer,
        IServiceProvider serviceProvider,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository;
        _kafkaProducer = kafkaProducer;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<PaginatedResponse<WorkflowListItemResponse>> GetWorkflowsAsync(int organizationId, int page, int pageSize, string? search = null, List<string>? statuses = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var workflows = await _workflowRepository.GetAllByOrganizationAsync(organizationId, page, pageSize, search, statuses);
        var totalCount = await _workflowRepository.GetTotalCountByOrganizationAsync(organizationId, search, statuses);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<WorkflowListItemResponse>
        {
            Data = workflows.Select(w => w.ToListItemDto()).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<WorkflowResponse> GetWorkflowByIdAsync(int id)
    {
        var workflow = await _workflowRepository.GetByIdWithDetailsAsync(id);

        if (workflow == null)
        {
            throw new NotFoundException("Workflow", id);
        }

        return workflow.ToDto();
    }

    public async Task<WorkflowResponse> CreateWorkflowAsync(CreateWorkflowRequest request, int organizationId, string userId)
    {
        // Validate workflow type
        if (request.Type != WorkflowType.QuoteFirst && request.Type != WorkflowType.InvoiceFirst)
        {
            throw new BusinessRuleException($"Invalid workflow type: {request.Type}. Must be '{WorkflowType.QuoteFirst}' or '{WorkflowType.InvoiceFirst}'");
        }

        // Validate linked documents based on type
        if (request.Type == WorkflowType.QuoteFirst && request.QuoteId == null)
        {
            throw new BusinessRuleException("QuoteFirst workflows must have a linked QuoteId");
        }

        if (request.Type == WorkflowType.InvoiceFirst && request.InvoiceId == null)
        {
            throw new BusinessRuleException("InvoiceFirst workflows must have a linked InvoiceId");
        }

        // Determine initial event type and status
        var initialEventType = request.Type == WorkflowType.QuoteFirst
            ? WorkflowEventType.QuoteCreated
            : WorkflowEventType.InvoiceCreated;

        var initialStatus = request.Type == WorkflowType.QuoteFirst
            ? WorkflowStatus.Draft
            : WorkflowStatus.InvoiceCreated;

        var workflow = new WorkflowModel
        {
            Status = initialStatus,
            Type = request.Type,
            OrganizationId = organizationId,
            QuoteId = request.QuoteId,
            InvoiceId = request.InvoiceId,
            ClientId = request.ClientId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            IsActive = true,
            Events = new List<WorkflowEvent>
            {
                new WorkflowEvent
                {
                    EventType = initialEventType,
                    Description = request.Type == WorkflowType.QuoteFirst
                        ? "Quote created — workflow started"
                        : "Invoice created — workflow started",
                    PerformedBy = userId,
                    OccurredAt = DateTime.UtcNow
                }
            }
        };

        var created = await _workflowRepository.AddAsync(workflow);

        _logger.LogInformation(
            "Workflow {WorkflowId} created (type: {Type}) for org {OrgId} by {User}",
            created.Id, request.Type, organizationId, userId);

        // Re-fetch with details for the response
        var result = await _workflowRepository.GetByIdWithDetailsAsync(created.Id);
        return result!.ToDto();
    }

    public async Task<WorkflowResponse> AddEventAsync(int workflowId, AddWorkflowEventRequest request, string userId)
    {
        var workflow = await _workflowRepository.GetByIdWithDetailsAsync(workflowId);

        if (workflow == null)
        {
            throw new NotFoundException("Workflow", workflowId);
        }

        // Check if workflow is active
        if (!workflow.IsActive)
        {
            throw new BusinessRuleException("Cannot add events to an inactive workflow");
        }

        // Validate event type is known
        if (!EventToStatusMap.ContainsKey(request.EventType))
        {
            throw new BusinessRuleException($"Unknown event type: {request.EventType}");
        }

        // Get the target status for this event
        var targetStatus = EventToStatusMap[request.EventType];

        // Special cases: events that record activity without changing status
        if (request.EventType == WorkflowEventType.QuoteModified)
        {
            // QuoteModified is only valid when status is Rejected
            if (workflow.Status != WorkflowStatus.Rejected)
            {
                throw new BusinessRuleException(
                    $"Cannot modify quote when workflow status is '{workflow.Status}'. Quote modification is only allowed when status is '{WorkflowStatus.Rejected}'");
            }
        }
        else if (request.EventType == WorkflowEventType.OverdueReminderSent)
        {
            // OverdueReminderSent is only valid when status is SentForPayment
            if (workflow.Status != WorkflowStatus.SentForPayment)
            {
                throw new BusinessRuleException(
                    $"Cannot send overdue reminder when workflow status is '{workflow.Status}'. Overdue reminders can only be sent when status is '{WorkflowStatus.SentForPayment}'");
            }
        }
        else
        {
            // Validate the status transition
            ValidateStatusTransition(workflow.Status, targetStatus, request.EventType);
        }

        // Add the event
        var workflowEvent = new WorkflowEvent
        {
            WorkflowId = workflowId,
            EventType = request.EventType,
            Description = request.Description,
            PerformedBy = userId,
            OccurredAt = DateTime.UtcNow
        };

        workflow.Events.Add(workflowEvent);

        // Update workflow status (QuoteModified and OverdueReminderSent leave status unchanged)
        if (request.EventType != WorkflowEventType.QuoteModified &&
            request.EventType != WorkflowEventType.OverdueReminderSent)
        {
            workflow.Status = targetStatus;
        }

        workflow.UpdatedAt = DateTime.UtcNow;

        await _workflowRepository.UpdateAsync(workflow);

        _logger.LogInformation(
            "Event '{EventType}' added to workflow {WorkflowId}. New status: {Status}. By {User}",
            request.EventType, workflowId, workflow.Status, userId);

        // Gate: ResentForApproval requires a QuoteModified event after the last Rejected event
        if (request.EventType == WorkflowEventType.ResentForApproval)
        {
            var lastRejectedAt = workflow.Events
                .Where(e => e.EventType == WorkflowEventType.Rejected)
                .OrderByDescending(e => e.OccurredAt)
                .FirstOrDefault()?.OccurredAt;

            var hasModifiedAfterRejection = workflow.Events.Any(e =>
                e.EventType == WorkflowEventType.QuoteModified &&
                (lastRejectedAt == null || e.OccurredAt > lastRejectedAt));

            if (!hasModifiedAfterRejection)
            {
                throw new BusinessRuleException(
                    "The quote must be edited before resending for approval. " +
                    "Please update the quote to address the client's concerns, then try again.");
            }
        }

        // Publish Kafka event for email notification when quote is sent for approval
        if (request.EventType == WorkflowEventType.SentForApproval ||
            request.EventType == WorkflowEventType.ResentForApproval)
        {
            try
            {
                if (workflow.QuoteId.HasValue)
                {
                    await _kafkaProducer.PublishQuoteApprovalRequestedEventAsync(
                        workflow.QuoteId.Value, workflowId);

                    _logger.LogInformation(
                        "Quote approval requested event published for QuoteId: {QuoteId}, WorkflowId: {WorkflowId}",
                        workflow.QuoteId.Value, workflowId);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot publish quote approval event — workflow {WorkflowId} has no linked QuoteId",
                        workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish quote approval requested event for WorkflowId: {WorkflowId}. " +
                    "Workflow status updated but email notification not triggered.",
                    workflowId);
            }
        }

        // Publish Kafka event for email notification when invoice is sent for payment
        if (request.EventType == WorkflowEventType.SentForPayment ||
            request.EventType == WorkflowEventType.ResentForPayment)
        {
            try
            {
                if (workflow.InvoiceId.HasValue)
                {
                    await _kafkaProducer.PublishInvoiceGeneratedEventAsync(
                        workflow.InvoiceId.Value, workflowId);

                    _logger.LogInformation(
                        "Invoice generated event published for InvoiceId: {InvoiceId}, WorkflowId: {WorkflowId}",
                        workflow.InvoiceId.Value, workflowId);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot publish invoice generated event — workflow {WorkflowId} has no linked InvoiceId",
                        workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish invoice generated event for WorkflowId: {WorkflowId}. " +
                    "Workflow status updated but email notification not triggered.",
                    workflowId);
            }
        }

        // Publish overdue Kafka event so EmailNotificationService sends the reminder email
        if (request.EventType == WorkflowEventType.OverdueReminderSent)
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
                        "Cannot publish overdue reminder event — workflow {WorkflowId} has no linked InvoiceId",
                        workflowId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish overdue reminder event for WorkflowId: {WorkflowId}. " +
                    "Workflow event recorded but email notification not triggered.",
                    workflowId);
            }
        }

        // Auto-create invoice when quote is converted to invoice
        if (request.EventType == WorkflowEventType.ConvertedToInvoice)
        {
            if (workflow.QuoteId.HasValue)
            {
                try
                {
                    // Resolve IInvoiceService lazily to avoid circular dependency
                    var invoiceService = _serviceProvider.GetRequiredService<IInvoiceService>();
                    var invoiceResult = await invoiceService.ConvertQuoteToInvoiceAsync(
                        new ConvertQuoteToInvoiceRequest
                        {
                            QuoteId = workflow.QuoteId.Value,
                            PayByDays = request.PayByDays
                        },
                        userId,
                        workflow.OrganizationId);

                    // Link the created invoice to the workflow
                    workflow.InvoiceId = invoiceResult.Id;
                    await _workflowRepository.UpdateAsync(workflow);

                    _logger.LogInformation(
                        "Invoice {InvoiceId} created from Quote {QuoteId} for Workflow {WorkflowId}",
                        invoiceResult.Id, workflow.QuoteId.Value, workflowId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to convert Quote {QuoteId} to Invoice for Workflow {WorkflowId}. " +
                        "Workflow status updated but invoice was not created.",
                        workflow.QuoteId, workflowId);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Cannot convert to invoice — workflow {WorkflowId} has no linked QuoteId",
                    workflowId);
            }
        }

        // Re-fetch with details for the response
        var result = await _workflowRepository.GetByIdWithDetailsAsync(workflowId);
        return result!.ToDto();
    }

    public async Task<WorkflowResponse> CancelWorkflowAsync(int id, string userId)
    {
        return await AddEventAsync(id, new AddWorkflowEventRequest
        {
            EventType = WorkflowEventType.Cancelled,
            Description = "Workflow cancelled"
        }, userId);
    }

    public async Task<WorkflowResponse> TerminateWorkflowAsync(int id, string userId)
    {
        return await AddEventAsync(id, new AddWorkflowEventRequest
        {
            EventType = WorkflowEventType.Terminated,
            Description = "Workflow terminated"
        }, userId);
    }

    public async Task<WorkflowResponse?> GetWorkflowByQuoteIdAsync(int quoteId)
    {
        var workflow = await _workflowRepository.GetByQuoteIdAsync(quoteId);
        return workflow?.ToDto();
    }

    public async Task<WorkflowResponse?> GetWorkflowByInvoiceIdAsync(int invoiceId)
    {
        var workflow = await _workflowRepository.GetByInvoiceIdAsync(invoiceId);
        return workflow?.ToDto();
    }

    /// <summary>
    /// Validates that a status transition is allowed according to the state machine
    /// </summary>
    private void ValidateStatusTransition(string currentStatus, string targetStatus, string eventType)
    {
        if (!ValidTransitions.TryGetValue(currentStatus, out var allowedTransitions))
        {
            throw new BusinessRuleException($"Unknown workflow status: {currentStatus}");
        }

        if (!allowedTransitions.Contains(targetStatus))
        {
            throw new BusinessRuleException(
                $"Invalid workflow transition: cannot move from '{currentStatus}' to '{targetStatus}' via event '{eventType}'. " +
                $"Allowed transitions from '{currentStatus}': [{string.Join(", ", allowedTransitions)}]");
        }
    }
}
