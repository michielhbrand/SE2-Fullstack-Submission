using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Workflow.Responses;
using Shared.Core.Exceptions.Application;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Workflow;
using Shared.Database.Models;
using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Services.Workflow;

/// <summary>
/// Service implementation for Workflow business logic with state machine validation
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowEventDispatcher _dispatcher;
    private readonly IQuoteToInvoiceConversionService _conversionService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IWorkflowEventDispatcher dispatcher,
        IQuoteToInvoiceConversionService conversionService,
        TimeProvider timeProvider,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository;
        _dispatcher = dispatcher;
        _conversionService = conversionService;
        _timeProvider = timeProvider;
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
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
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
                    OccurredAt = _timeProvider.GetUtcNow().UtcDateTime
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
        if (!WorkflowStateMachine.IsKnownEventType(request.EventType))
        {
            throw new BusinessRuleException($"Unknown event type: {request.EventType}");
        }

        // Get the target status for this event
        var targetStatus = WorkflowStateMachine.GetTargetStatus(request.EventType)!;

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
            OccurredAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        workflow.Events.Add(workflowEvent);

        // Update workflow status (QuoteModified and OverdueReminderSent leave status unchanged)
        if (request.EventType != WorkflowEventType.QuoteModified &&
            request.EventType != WorkflowEventType.OverdueReminderSent)
        {
            workflow.Status = targetStatus;
        }

        workflow.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

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

        // Dispatch Kafka events for email notifications
        await _dispatcher.DispatchAsync(workflowEvent, workflow);

        // Auto-create invoice when quote is converted to invoice
        if (request.EventType == WorkflowEventType.ConvertedToInvoice)
        {
            if (workflow.QuoteId.HasValue)
            {
                try
                {
                    var invoiceResult = await _conversionService.ConvertAsync(
                        workflow.QuoteId.Value,
                        request.PayByDays,
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
    private static void ValidateStatusTransition(string currentStatus, string targetStatus, string eventType)
    {
        if (!WorkflowStateMachine.IsValidTransition(currentStatus, targetStatus))
        {
            throw new BusinessRuleException(
                $"Invalid workflow transition: cannot move from '{currentStatus}' to '{targetStatus}' via event '{eventType}'.");
        }
    }
}
