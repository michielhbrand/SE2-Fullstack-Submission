using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Workflow.Responses;

namespace InvoiceTrackerApi.Services.Workflow;

/// <summary>
/// Service interface for Workflow business logic
/// </summary>
public interface IWorkflowService
{
    Task<PaginatedResponse<WorkflowListItemResponse>> GetWorkflowsAsync(int organizationId, int page, int pageSize);
    Task<WorkflowResponse> GetWorkflowByIdAsync(int id);
    Task<WorkflowResponse> CreateWorkflowAsync(CreateWorkflowRequest request, int organizationId, string userId);
    Task<WorkflowResponse> AddEventAsync(int workflowId, AddWorkflowEventRequest request, string userId);
    Task<WorkflowResponse> CancelWorkflowAsync(int id, string userId);
    Task<WorkflowResponse> TerminateWorkflowAsync(int id, string userId);
    Task<WorkflowResponse?> GetWorkflowByQuoteIdAsync(int quoteId);
    Task<WorkflowResponse?> GetWorkflowByInvoiceIdAsync(int invoiceId);
}
