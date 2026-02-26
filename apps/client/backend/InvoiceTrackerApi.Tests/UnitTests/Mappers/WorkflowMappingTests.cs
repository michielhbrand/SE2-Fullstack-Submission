using FluentAssertions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Tests.Helpers;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.UnitTests.Mappers;

public class WorkflowMappingTests
{
    [Fact]
    public void Workflow_ToDto_MapsStatusAndType()
    {
        var workflow = TestDataBuilder.CreateWorkflow(
            id: 10,
            status: WorkflowStatus.PendingApproval,
            type: WorkflowType.QuoteFirst,
            quoteId: 5);

        var dto = workflow.ToDto();

        dto.Id.Should().Be(10);
        dto.Status.Should().Be(WorkflowStatus.PendingApproval);
        dto.Type.Should().Be(WorkflowType.QuoteFirst);
        dto.QuoteId.Should().Be(5);
    }

    [Fact]
    public void Workflow_ToDto_MapsClientName_WhenClientIsLoaded()
    {
        var workflow = TestDataBuilder.CreateWorkflow(id: 1, clientId: 3);
        workflow.Client = TestDataBuilder.CreateClient(id: 3);

        var dto = workflow.ToDto();

        dto.ClientName.Should().Be("Test Client");
        dto.ClientEmail.Should().Be("client@test.com");
    }

    [Fact]
    public void Workflow_ToListItemDto_MapsStatusAndClientName()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Approved);
        workflow.Client = TestDataBuilder.CreateClient();

        var dto = workflow.ToListItemDto();

        dto.Status.Should().Be(WorkflowStatus.Approved);
        dto.ClientName.Should().Be("Test Client");
    }
}
