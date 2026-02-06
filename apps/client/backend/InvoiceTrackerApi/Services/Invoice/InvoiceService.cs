using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Invoice;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceModel = InvoiceTrackerApi.Models.Invoice;

namespace InvoiceTrackerApi.Services.Invoice;

/// <summary>
/// Service implementation for Invoice business logic
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IPdfStorageService _pdfStorageService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IKafkaProducerService kafkaProducer,
        IPdfStorageService pdfStorageService,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _kafkaProducer = kafkaProducer;
        _pdfStorageService = pdfStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResponse<InvoiceResponse>> GetInvoicesAsync(int page, int pageSize)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var invoices = await _invoiceRepository.GetAllAsync(page, pageSize);
        var totalCount = await _invoiceRepository.GetTotalCountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<InvoiceResponse>
        {
            Data = invoices.Select(i => i.ToDto()).ToList(),
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

    public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, string modifiedBy)
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
            DateCreated = DateTime.UtcNow,
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
        // Note: If Kafka publishing fails, we keep the invoice in the database
        // The PDF can be regenerated later via a retry mechanism or manual intervention
        try
        {
            await _kafkaProducer.PublishInvoiceCreatedEventAsync(createdInvoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish Kafka event for Invoice {InvoiceId}. Invoice saved but PDF generation not triggered. Manual intervention may be required.",
                createdInvoice.Id);
            
            // We do NOT delete the invoice - it remains in the database without a PDF
            // The PdfStorageKey will be null, indicating PDF generation is pending/failed
            // This allows for retry mechanisms or manual PDF generation later
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
}
