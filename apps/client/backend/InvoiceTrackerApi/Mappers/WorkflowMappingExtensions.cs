using InvoiceTrackerApi.DTOs.Workflow.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between Workflow domain models and DTOs
/// </summary>
public static class WorkflowMappingExtensions
{
    public static WorkflowResponse ToDto(this Workflow workflow)
    {
        return new WorkflowResponse
        {
            Id = workflow.Id,
            Status = workflow.Status,
            Type = workflow.Type,
            OrganizationId = workflow.OrganizationId,
            QuoteId = workflow.QuoteId,
            InvoiceId = workflow.InvoiceId,
            ClientId = workflow.ClientId,
            ClientName = workflow.Client != null
                ? $"{workflow.Client.Name} {workflow.Client.Surname}"
                : null,
            ClientEmail = workflow.Client?.Email,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            CreatedBy = workflow.CreatedBy,
            IsActive = workflow.IsActive,
            Events = workflow.Events.Select(e => e.ToDto()).ToList()
        };
    }

    public static WorkflowListItemResponse ToListItemDto(this Workflow workflow)
    {
        return new WorkflowListItemResponse
        {
            Id = workflow.Id,
            Status = workflow.Status,
            Type = workflow.Type,
            ClientId = workflow.ClientId,
            ClientName = workflow.Client != null
                ? $"{workflow.Client.Name} {workflow.Client.Surname}"
                : null,
            QuoteId = workflow.QuoteId,
            InvoiceId = workflow.InvoiceId,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            IsActive = workflow.IsActive
        };
    }

    public static WorkflowEventResponse ToDto(this WorkflowEvent workflowEvent)
    {
        return new WorkflowEventResponse
        {
            Id = workflowEvent.Id,
            WorkflowId = workflowEvent.WorkflowId,
            EventType = workflowEvent.EventType,
            Description = workflowEvent.Description,
            PerformedBy = workflowEvent.PerformedBy,
            OccurredAt = workflowEvent.OccurredAt
        };
    }
}
