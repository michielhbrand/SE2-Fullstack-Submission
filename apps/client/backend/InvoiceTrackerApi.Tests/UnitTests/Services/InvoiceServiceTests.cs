using FluentAssertions;
using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Invoice;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Services;
using InvoiceTrackerApi.Services.Invoice;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceTrackerApi.Services.Workflow;
using InvoiceTrackerApi.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Tests.UnitTests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock;
    private readonly Mock<IQuoteRepository> _quoteRepoMock;
    private readonly Mock<IClientRepository> _clientRepoMock;
    private readonly Mock<IKafkaProducerService> _kafkaMock;
    private readonly Mock<IPdfStorageService> _pdfStorageMock;
    private readonly Mock<IWorkflowService> _workflowServiceMock;
    private readonly Mock<ILogger<InvoiceService>> _loggerMock;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _invoiceRepoMock = new Mock<IInvoiceRepository>();
        _quoteRepoMock = new Mock<IQuoteRepository>();
        _clientRepoMock = new Mock<IClientRepository>();
        _kafkaMock = new Mock<IKafkaProducerService>();
        _pdfStorageMock = new Mock<IPdfStorageService>();
        _workflowServiceMock = new Mock<IWorkflowService>();
        _loggerMock = new Mock<ILogger<InvoiceService>>();

        _service = new InvoiceService(
            _invoiceRepoMock.Object,
            _quoteRepoMock.Object,
            _clientRepoMock.Object,
            _kafkaMock.Object,
            _pdfStorageMock.Object,
            _workflowServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateInvoice_EmptyItems_ThrowsBusinessRuleException()
    {
        _clientRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

        var request = new CreateInvoiceRequest
        {
            ClientId = 1,
            Items = new List<CreateInvoiceItemRequest>()
        };

        var act = () => _service.CreateInvoiceAsync(request, "user-1", 1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public async Task CreateInvoice_NonExistentClient_ThrowsBusinessRuleException()
    {
        _clientRepoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        var request = new CreateInvoiceRequest
        {
            ClientId = 99,
            Items = new List<CreateInvoiceItemRequest>
            {
                new CreateInvoiceItemRequest { Description = "Item", Quantity = 1, UnitPrice = 100 }
            }
        };

        var act = () => _service.CreateInvoiceAsync(request, "user-1", 1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*99*does not exist*");
    }

    [Fact]
    public async Task ConvertQuoteToInvoice_QuoteWithNoItems_ThrowsBusinessRuleException()
    {
        var emptyQuote = TestDataBuilder.CreateQuote(id: 10, items: new List<QuoteItem>());
        _quoteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(emptyQuote);

        var request = new ConvertQuoteToInvoiceRequest { QuoteId = 10, PayByDays = 30 };

        var act = () => _service.ConvertQuoteToInvoiceAsync(request, "user-1", 1);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*no items*");
    }
}
