using FluentAssertions;
using InvoiceTrackerApi.DTOs.Quote.Requests;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Services;
using InvoiceTrackerApi.Services.Quote;
using InvoiceTrackerApi.Services.Workflow;
using InvoiceTrackerApi.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace InvoiceTrackerApi.Tests.UnitTests.Services;

public class QuoteServiceTests
{
    private readonly Mock<IQuoteRepository> _quoteRepoMock;
    private readonly Mock<IClientRepository> _clientRepoMock;
    private readonly Mock<IKafkaProducerService> _kafkaMock;
    private readonly Mock<IWorkflowService> _workflowServiceMock;
    private readonly Mock<ILogger<QuoteService>> _loggerMock;
    private readonly QuoteService _service;

    public QuoteServiceTests()
    {
        _quoteRepoMock = new Mock<IQuoteRepository>();
        _clientRepoMock = new Mock<IClientRepository>();
        _kafkaMock = new Mock<IKafkaProducerService>();
        _workflowServiceMock = new Mock<IWorkflowService>();
        _loggerMock = new Mock<ILogger<QuoteService>>();

        // PdfStorageService is not needed for these unit tests
        var pdfStorageMock = new Mock<InvoiceTrackerApi.Services.PdfStorage.IPdfStorageService>();

        _service = new QuoteService(
            _quoteRepoMock.Object,
            _clientRepoMock.Object,
            _kafkaMock.Object,
            pdfStorageMock.Object,
            _workflowServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateQuote_EmptyItems_ThrowsBusinessRuleException()
    {
        _clientRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

        var request = new CreateQuoteRequest
        {
            ClientId = 1,
            Items = new List<CreateQuoteItemRequest>()
        };

        var act = () => _service.CreateQuoteAsync(request, "user-1", 1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public async Task CreateQuote_NonExistentClient_ThrowsBusinessRuleException()
    {
        _clientRepoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        var request = new CreateQuoteRequest
        {
            ClientId = 99,
            Items = new List<CreateQuoteItemRequest>
            {
                new CreateQuoteItemRequest { Description = "Item", Quantity = 1, UnitPrice = 100 }
            }
        };

        var act = () => _service.CreateQuoteAsync(request, "user-1", 1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*99*does not exist*");
    }

    [Fact]
    public async Task CreateQuote_ValidRequest_PublishesKafkaEvent()
    {
        var quote = TestDataBuilder.CreateQuote(id: 5);
        _clientRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _quoteRepoMock.Setup(r => r.AddAsync(It.IsAny<Shared.Database.Models.Quote>()))
            .ReturnsAsync(quote);
        _kafkaMock.Setup(k => k.PublishQuoteCreatedEventAsync(5)).Returns(Task.CompletedTask);
        _workflowServiceMock.Setup(w => w.CreateWorkflowAsync(It.IsAny<InvoiceTrackerApi.DTOs.Workflow.Requests.CreateWorkflowRequest>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new InvoiceTrackerApi.DTOs.Workflow.Responses.WorkflowResponse());

        var request = new CreateQuoteRequest
        {
            ClientId = 1,
            Items = new List<CreateQuoteItemRequest>
            {
                new CreateQuoteItemRequest { Description = "Consulting", Quantity = 2, UnitPrice = 500 }
            }
        };

        await _service.CreateQuoteAsync(request, "user-1", 1);

        _kafkaMock.Verify(k => k.PublishQuoteCreatedEventAsync(5), Times.Once);
    }

    [Fact]
    public async Task UpdateQuote_PublishesKafkaEventForPdfRegeneration()
    {
        var existing = TestDataBuilder.CreateQuote(id: 3);
        _quoteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(3)).ReturnsAsync(existing);
        _clientRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _quoteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Shared.Database.Models.Quote>())).Returns(Task.CompletedTask);
        _kafkaMock.Setup(k => k.PublishQuoteCreatedEventAsync(3)).Returns(Task.CompletedTask);

        var request = new UpdateQuoteRequest
        {
            ClientId = 1,
            VatInclusive = true,
            Items = new List<CreateQuoteItemRequest>
            {
                new CreateQuoteItemRequest { Description = "Updated item", Quantity = 1, UnitPrice = 200 }
            }
        };

        await _service.UpdateQuoteAsync(3, request, "user-1");

        _kafkaMock.Verify(k => k.PublishQuoteCreatedEventAsync(3), Times.Once);
    }
}
