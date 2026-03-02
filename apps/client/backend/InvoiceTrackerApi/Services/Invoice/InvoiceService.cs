using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Common;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using InvoiceTrackerApi.Mappers;
using Shared.Database.Models;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Invoice;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceTrackerApi.Services.Workflow;
using InvoiceModel = Shared.Database.Models.Invoice;

namespace InvoiceTrackerApi.Services.Invoice;

/// <summary>
/// Service implementation for Invoice business logic
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IPdfStorageService _pdfStorageService;
    private readonly IWorkflowService _workflowService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IQuoteRepository quoteRepository,
        IClientRepository clientRepository,
        IKafkaProducerService kafkaProducer,
        IPdfStorageService pdfStorageService,
        IWorkflowService workflowService,
        IConfiguration configuration,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _quoteRepository = quoteRepository;
        _clientRepository = clientRepository;
        _kafkaProducer = kafkaProducer;
        _pdfStorageService = pdfStorageService;
        _workflowService = workflowService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaginatedResponse<InvoiceResponse>> GetInvoicesAsync(int organizationId, int page, int pageSize, string? statusFilter = null, string? search = null)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var invoices = await _invoiceRepository.GetAllAsync(organizationId, page, pageSize, statusFilter, search);
        var totalCount = await _invoiceRepository.GetTotalCountAsync(organizationId, statusFilter, search);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<InvoiceResponse>
        {
            Data = invoices.Select(t => t.Invoice.ToDto(t.WorkflowStatus)).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<InvoiceResponse> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(id);

        if (invoice == null)
        {
            throw new NotFoundException("Invoice", id);
        }

        return invoice.ToDto();
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, string modifiedBy, int organizationId)
    {
        // Business rule validation: Check if client exists
        var clientExists = await _clientRepository.ExistsAsync(request.ClientId);
        if (!clientExists)
        {
            throw new BusinessRuleException($"Client with ID {request.ClientId} does not exist");
        }

        // Business rule validation: Ensure at least one item
        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleException("Invoice must contain at least one item");
        }

        var invoice = new InvoiceModel
        {
            ClientId = request.ClientId,
            TemplateId = request.TemplateId,
            VatInclusive = request.VatInclusive,
            OrganizationId = organizationId,
            DateCreated = DateTime.UtcNow,
            PayByDate = DateTime.UtcNow.AddDays(request.PayByDays),
            ModifiedBy = modifiedBy,
            LastModifiedDate = DateTime.UtcNow,
            Items = request.Items.Select(item => new InvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                PricePerUnit = item.UnitPrice
            }).ToList()
        };

        var createdInvoice = await _invoiceRepository.AddAsync(invoice);

        _logger.LogInformation("Invoice {InvoiceId} created by {User}", createdInvoice.Id, modifiedBy);

        // Publish Kafka event for PDF generation
        try
        {
            await _kafkaProducer.PublishInvoiceCreatedEventAsync(createdInvoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish Kafka event for Invoice {InvoiceId}. Invoice saved but PDF generation not triggered.",
                createdInvoice.Id);
        }

        // Auto-create InvoiceFirst workflow
        try
        {
            await _workflowService.CreateWorkflowAsync(new CreateWorkflowRequest
            {
                Type = WorkflowType.InvoiceFirst,
                ClientId = request.ClientId,
                InvoiceId = createdInvoice.Id
            }, organizationId, modifiedBy);

            _logger.LogInformation("Workflow auto-created for Invoice {InvoiceId}", createdInvoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to auto-create workflow for Invoice {InvoiceId}. Invoice exists but workflow was not created.",
                createdInvoice.Id);
        }

        return createdInvoice.ToDto();
    }

    public async Task<InvoiceResponse> UpdateInvoiceAsync(int id, UpdateInvoiceRequest request, string modifiedBy)
    {
        var existingInvoice = await _invoiceRepository.GetByIdWithDetailsAsync(id);

        if (existingInvoice == null)
        {
            throw new NotFoundException("Invoice", id);
        }

        // Business rule validation: Check if client exists
        var clientExists = await _clientRepository.ExistsAsync(request.ClientId);
        if (!clientExists)
        {
            throw new BusinessRuleException($"Client with ID {request.ClientId} does not exist");
        }

        // Business rule validation: Ensure at least one item
        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleException("Invoice must contain at least one item");
        }

        // Update properties
        existingInvoice.ClientId = request.ClientId;
        existingInvoice.NotificationSent = request.NotificationSent;
        existingInvoice.TemplateId = request.TemplateId;
        existingInvoice.VatInclusive = request.VatInclusive;
        existingInvoice.LastModifiedDate = DateTime.UtcNow;
        existingInvoice.ModifiedBy = modifiedBy;

        // Update items - clear and recreate
        existingInvoice.Items.Clear();
        existingInvoice.Items = request.Items.Select(item => new InvoiceItem
        {
            InvoiceId = id,
            Description = item.Description,
            Quantity = item.Quantity,
            PricePerUnit = item.UnitPrice
        }).ToList();

        await _invoiceRepository.UpdateAsync(existingInvoice);

        _logger.LogInformation("Invoice {InvoiceId} updated by {User}", id, modifiedBy);

        return existingInvoice.ToDto();
    }

    public async Task DeleteInvoiceAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(id);

        if (invoice == null)
        {
            throw new NotFoundException("Invoice", id);
        }

        await _invoiceRepository.DeleteAsync(invoice);

        _logger.LogInformation("Invoice {InvoiceId} deleted", id);
    }

    public async Task<string?> GetInvoicePdfUrlAsync(int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id);

        if (invoice == null)
        {
            throw new NotFoundException("Invoice", id);
        }

        if (string.IsNullOrEmpty(invoice.PdfStorageKey))
        {
            throw new BusinessRuleException("PDF not yet generated for this invoice");
        }

        return await _pdfStorageService.GetPresignedUrlAsync(invoice.PdfStorageKey);
    }

    public async Task<InvoiceResponse> ConvertQuoteToInvoiceAsync(ConvertQuoteToInvoiceRequest request, string modifiedBy, int organizationId)
    {
        // Fetch the quote with details
        var quote = await _quoteRepository.GetByIdWithDetailsAsync(request.QuoteId);

        if (quote == null)
        {
            throw new NotFoundException("Quote", request.QuoteId);
        }

        // Business rule: quote must have items
        if (quote.Items == null || !quote.Items.Any())
        {
            throw new BusinessRuleException("Cannot convert a quote with no items to an invoice");
        }

        // Create invoice from quote data
        // Do NOT inherit the quote's TemplateId — it's a Quote-type template and
        // would produce a PDF with quote placeholders/footer instead of invoice ones.
        var invoice = new InvoiceModel
        {
            ClientId = quote.ClientId,
            TemplateId = request.TemplateId,
            VatInclusive = request.VatInclusive ?? quote.VatInclusive,
            OrganizationId = organizationId,
            DateCreated = DateTime.UtcNow,
            PayByDate = DateTime.UtcNow.AddDays(request.PayByDays),
            ModifiedBy = modifiedBy,
            LastModifiedDate = DateTime.UtcNow,
            Items = quote.Items.Select(item => new InvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                PricePerUnit = item.PricePerUnit
            }).ToList()
        };

        var createdInvoice = await _invoiceRepository.AddAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} created from Quote {QuoteId} by {User}",
            createdInvoice.Id, request.QuoteId, modifiedBy);

        // Publish Kafka event for PDF generation
        try
        {
            await _kafkaProducer.PublishInvoiceCreatedEventAsync(createdInvoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish Kafka event for Invoice {InvoiceId} (converted from Quote {QuoteId}). Invoice saved but PDF generation not triggered.",
                createdInvoice.Id, request.QuoteId);
        }

        return createdInvoice.ToDto();
    }

    public async Task<int> ProcessOverdueInvoicesAsync(int? organizationId, CancellationToken ct = default)
    {
        var reminderIntervalDays = int.TryParse(
            _configuration["OverdueInvoice:ReminderIntervalDays"], out var days) ? days : 7;

        var overdue = (await _invoiceRepository.GetOverdueAsync(
            DateTime.UtcNow, reminderIntervalDays, organizationId, ct)).ToList();

        foreach (var (invoice, workflowId) in overdue)
        {
            try
            {
                await _kafkaProducer.PublishInvoiceOverdueEventAsync(invoice.Id, workflowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish overdue event for Invoice {InvoiceId}. Skipping.",
                    invoice.Id);
            }
        }

        _logger.LogInformation("Overdue processing complete. {Count} invoice(s) queued for reminder.", overdue.Count);
        return overdue.Count;
    }
}
