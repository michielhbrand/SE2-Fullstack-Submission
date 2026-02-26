using FluentAssertions;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Repositories.Workflow;
using InvoiceTrackerApi.Services;
using InvoiceTrackerApi.Services.Workflow;
using InvoiceTrackerApi.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Database.Models;
using WorkflowModel = Shared.Database.Models.Workflow;

namespace InvoiceTrackerApi.Tests.UnitTests.Services;

public class WorkflowServiceTests
{
    private readonly Mock<IWorkflowRepository> _workflowRepoMock;
    private readonly Mock<IKafkaProducerService> _kafkaMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<WorkflowService>> _loggerMock;
    private readonly WorkflowService _service;

    public WorkflowServiceTests()
    {
        _workflowRepoMock = new Mock<IWorkflowRepository>();
        _kafkaMock = new Mock<IKafkaProducerService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<WorkflowService>>();

        _service = new WorkflowService(
            _workflowRepoMock.Object,
            _kafkaMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object);
    }

    // ─── CreateWorkflow validation ────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkflow_QuoteFirst_WithoutQuoteId_ThrowsBusinessRuleException()
    {
        var request = new CreateWorkflowRequest
        {
            Type = WorkflowType.QuoteFirst,
            ClientId = 1,
            QuoteId = null
        };

        var act = () => _service.CreateWorkflowAsync(request, 1, "user-1");

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*QuoteFirst*QuoteId*");
    }

    [Fact]
    public async Task CreateWorkflow_InvoiceFirst_WithoutInvoiceId_ThrowsBusinessRuleException()
    {
        var request = new CreateWorkflowRequest
        {
            Type = WorkflowType.InvoiceFirst,
            ClientId = 1,
            InvoiceId = null
        };

        var act = () => _service.CreateWorkflowAsync(request, 1, "user-1");

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*InvoiceFirst*InvoiceId*");
    }

    [Fact]
    public async Task CreateWorkflow_QuoteFirst_CreatesWorkflowWithDraftStatus()
    {
        var savedWorkflow = TestDataBuilder.CreateWorkflow(id: 1, status: WorkflowStatus.Draft, quoteId: 10);
        _workflowRepoMock.Setup(r => r.AddAsync(It.IsAny<WorkflowModel>()))
            .ReturnsAsync(savedWorkflow);
        _workflowRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(savedWorkflow);

        var request = new CreateWorkflowRequest
        {
            Type = WorkflowType.QuoteFirst,
            ClientId = 1,
            QuoteId = 10
        };

        var result = await _service.CreateWorkflowAsync(request, 1, "user-1");

        result.Status.Should().Be(WorkflowStatus.Draft);
        result.Type.Should().Be(WorkflowType.QuoteFirst);
    }

    // ─── AddEvent — happy-path transitions ───────────────────────────────────

    [Fact]
    public async Task AddEvent_SentForApproval_TransitionsToPendingApproval()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Draft, quoteId: 10);
        SetupWorkflowRepo(workflow);

        var result = await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.SentForApproval }, "user-1");

        result.Status.Should().Be(WorkflowStatus.PendingApproval);
    }

    [Fact]
    public async Task AddEvent_Approved_TransitionsToApproved()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.PendingApproval);
        SetupWorkflowRepo(workflow);

        var result = await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.Approved }, "user-1");

        result.Status.Should().Be(WorkflowStatus.Approved);
    }

    [Fact]
    public async Task AddEvent_Rejected_TransitionsToRejected()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.PendingApproval);
        SetupWorkflowRepo(workflow);

        var result = await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.Rejected }, "user-1");

        result.Status.Should().Be(WorkflowStatus.Rejected);
    }

    [Fact]
    public async Task AddEvent_MarkedAsPaid_TransitionsToPaid()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.SentForPayment, invoiceId: 5);
        SetupWorkflowRepo(workflow);

        var result = await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.MarkedAsPaid }, "user-1");

        result.Status.Should().Be(WorkflowStatus.Paid);
    }

    // ─── AddEvent — invalid transitions ──────────────────────────────────────

    [Theory]
    [InlineData(WorkflowStatus.Paid)]
    [InlineData(WorkflowStatus.Cancelled)]
    [InlineData(WorkflowStatus.Terminated)]
    public async Task AddEvent_FromTerminalStatus_ThrowsBusinessRuleException(string terminalStatus)
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: terminalStatus);
        SetupWorkflowRepo(workflow);

        var act = () => _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.SentForApproval }, "user-1");

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task AddEvent_InvalidTransition_ThrowsBusinessRuleException()
    {
        // Cannot send for approval from Paid status
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Paid);
        SetupWorkflowRepo(workflow);

        var act = () => _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.SentForApproval }, "user-1");

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    // ─── AddEvent — QuoteModified rules ──────────────────────────────────────

    [Fact]
    public async Task AddEvent_QuoteModified_WhenNotRejected_ThrowsBusinessRuleException()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.PendingApproval);
        SetupWorkflowRepo(workflow);

        var act = () => _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.QuoteModified }, "user-1");

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Rejected*");
    }

    [Fact]
    public async Task AddEvent_QuoteModified_WhenRejected_DoesNotChangeStatus()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Rejected);
        SetupWorkflowRepo(workflow);

        var result = await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.QuoteModified, Description = "Adjusted pricing" }, "user-1");

        result.Status.Should().Be(WorkflowStatus.Rejected);
    }

    // ─── AddEvent — ResentForApproval gate ───────────────────────────────────

    [Fact]
    public async Task AddEvent_ResentForApproval_WithoutPriorQuoteModified_ThrowsBusinessRuleException()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Rejected, quoteId: 10);
        // No QuoteModified event in the history
        SetupWorkflowRepo(workflow);

        var act = () => _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.ResentForApproval }, "user-1");

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*edited*");
    }

    [Fact]
    public async Task AddEvent_ResentForApproval_WithPriorQuoteModified_TransitionsToPendingApproval()
    {
        var rejectedAt = DateTime.UtcNow.AddMinutes(-10);
        var modifiedAt = DateTime.UtcNow.AddMinutes(-5);

        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Rejected, quoteId: 10);
        workflow.Events = new List<WorkflowEvent>
        {
            new WorkflowEvent { EventType = WorkflowEventType.Rejected,      OccurredAt = rejectedAt },
            new WorkflowEvent { EventType = WorkflowEventType.QuoteModified, OccurredAt = modifiedAt }
        };
        SetupWorkflowRepo(workflow);

        _kafkaMock.Setup(k => k.PublishQuoteApprovalRequestedEventAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var result = await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.ResentForApproval }, "user-1");

        result.Status.Should().Be(WorkflowStatus.PendingApproval);
    }

    // ─── AddEvent — Kafka publishing ─────────────────────────────────────────

    [Fact]
    public async Task AddEvent_SentForApproval_PublishesKafkaEvent()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.Draft, quoteId: 10);
        SetupWorkflowRepo(workflow);
        _kafkaMock.Setup(k => k.PublishQuoteApprovalRequestedEventAsync(10, 1))
            .Returns(Task.CompletedTask);

        await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.SentForApproval }, "user-1");

        _kafkaMock.Verify(k => k.PublishQuoteApprovalRequestedEventAsync(10, 1), Times.Once);
    }

    [Fact]
    public async Task AddEvent_SentForPayment_PublishesKafkaEvent()
    {
        var workflow = TestDataBuilder.CreateWorkflow(status: WorkflowStatus.InvoiceCreated, invoiceId: 5);
        SetupWorkflowRepo(workflow);
        _kafkaMock.Setup(k => k.PublishInvoiceGeneratedEventAsync(5, 1))
            .Returns(Task.CompletedTask);

        await _service.AddEventAsync(1,
            new AddWorkflowEventRequest { EventType = WorkflowEventType.SentForPayment }, "user-1");

        _kafkaMock.Verify(k => k.PublishInvoiceGeneratedEventAsync(5, 1), Times.Once);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures the workflow repository mock to return the given workflow on every
    /// GetByIdWithDetailsAsync call. Because the service mutates the workflow object
    /// in memory before re-fetching, the second fetch naturally returns the updated object.
    /// </summary>
    private void SetupWorkflowRepo(WorkflowModel workflow)
    {
        _workflowRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>()))
            .ReturnsAsync(workflow);
        _workflowRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkflowModel>()))
            .Returns(Task.CompletedTask);
    }
}
