using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Workflow.Responses;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.Services.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Controllers;

[ApiController]
[Route("api/workflow")]
[Produces("application/json")]
[Authorize]
public class WorkflowController : AuthenticatedControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowService workflowService,
        ILogger<WorkflowController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of workflows for an organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <returns>Paginated list of workflows</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<WorkflowListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<WorkflowListItemResponse>>> GetWorkflows(
        [FromQuery] int organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? statuses = null)
    {
        var statusList = string.IsNullOrWhiteSpace(statuses)
            ? null
            : statuses.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        var response = await _workflowService.GetWorkflowsAsync(organizationId, page, pageSize, search, statusList);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific workflow by ID with all events
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Workflow details with timeline events</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> GetWorkflow(int id)
    {
        var workflow = await _workflowService.GetWorkflowByIdAsync(id);
        return Ok(workflow);
    }

    /// <summary>
    /// Create a new workflow
    /// </summary>
    /// <param name="request">Workflow creation data</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Created workflow</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> CreateWorkflow(
        [FromBody] CreateWorkflowRequest request,
        [FromQuery] int organizationId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserIdentifier();
        var workflow = await _workflowService.CreateWorkflowAsync(request, organizationId, userId);

        return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow);
    }

    /// <summary>
    /// Add an event to a workflow (advances the workflow state)
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="request">Event data</param>
    /// <returns>Updated workflow</returns>
    [HttpPost("{id}/events")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> AddEvent(int id, [FromBody] AddWorkflowEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserIdentifier();
        var workflow = await _workflowService.AddEventAsync(id, request, userId);

        return Ok(workflow);
    }

    /// <summary>
    /// Cancel a workflow
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Updated workflow</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> CancelWorkflow(int id)
    {
        var userId = GetCurrentUserIdentifier();
        var workflow = await _workflowService.CancelWorkflowAsync(id, userId);

        return Ok(workflow);
    }

    /// <summary>
    /// Terminate a workflow
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Updated workflow</returns>
    [HttpPost("{id}/terminate")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> TerminateWorkflow(int id)
    {
        var userId = GetCurrentUserIdentifier();
        var workflow = await _workflowService.TerminateWorkflowAsync(id, userId);

        return Ok(workflow);
    }

    /// <summary>
    /// Get workflow by quote ID
    /// </summary>
    /// <param name="quoteId">Quote ID</param>
    /// <returns>Workflow linked to the quote</returns>
    [HttpGet("by-quote/{quoteId}")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> GetWorkflowByQuote(int quoteId)
    {
        var workflow = await _workflowService.GetWorkflowByQuoteIdAsync(quoteId);

        if (workflow == null)
        {
            return NotFound(new { message = $"No workflow found for quote {quoteId}" });
        }

        return Ok(workflow);
    }

    /// <summary>
    /// Get workflow by invoice ID
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <returns>Workflow linked to the invoice</returns>
    [HttpGet("by-invoice/{invoiceId}")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowResponse>> GetWorkflowByInvoice(int invoiceId)
    {
        var workflow = await _workflowService.GetWorkflowByInvoiceIdAsync(invoiceId);

        if (workflow == null)
        {
            return NotFound(new { message = $"No workflow found for invoice {invoiceId}" });
        }

        return Ok(workflow);
    }
}
