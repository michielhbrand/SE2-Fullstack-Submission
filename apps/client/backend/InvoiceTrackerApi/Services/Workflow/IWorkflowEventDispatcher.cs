using Shared.Database.Models;
using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Services.Workflow;

public interface IWorkflowEventDispatcher
{
    Task DispatchAsync(WorkflowEvent workflowEvent, WorkflowModel workflow, CancellationToken ct = default);
}
