using InvoiceTrackerApi.DTOs.Quote.Requests;
using InvoiceTrackerApi.DTOs.Quote.Responses;
using InvoiceTrackerApi.DTOs.Workflow.Requests;
using InvoiceTrackerApi.DTOs.Common;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using InvoiceTrackerApi.Mappers;
using Shared.Database.Models;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Services.PdfStorage;
using InvoiceTrackerApi.Services.Workflow;
using QuoteModel = Shared.Database.Models.Quote;

namespace InvoiceTrackerApi.Services.Quote;

/// <summary>
/// Service implementation for Quote business logic
/// </summary>
public class QuoteService : IQuoteService
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IPdfStorageService _pdfStorageService;
    private readonly IWorkflowService _workflowService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        IQuoteRepository quoteRepository,
        IClientRepository clientRepository,
        IKafkaProducerService kafkaProducer,
        IPdfStorageService pdfStorageService,
        IWorkflowService workflowService,
        TimeProvider timeProvider,
        ILogger<QuoteService> logger)
    {
        _quoteRepository = quoteRepository;
        _clientRepository = clientRepository;
        _kafkaProducer = kafkaProducer;
        _pdfStorageService = pdfStorageService;
        _workflowService = workflowService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PaginatedResponse<QuoteResponse>> GetQuotesAsync(int organizationId, int page, int pageSize)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var quotes = await _quoteRepository.GetAllAsync(organizationId, page, pageSize);
        var totalCount = await _quoteRepository.GetTotalCountAsync(organizationId);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<QuoteResponse>
        {
            Data = quotes.Select(q => q.ToDto()).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<QuoteResponse> GetQuoteByIdAsync(int id)
    {
        var quote = await _quoteRepository.GetByIdWithDetailsAsync(id);

        if (quote == null)
        {
            throw new NotFoundException("Quote", id);
        }

        return quote.ToDto();
    }

    public async Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, string modifiedBy, int organizationId)
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
            throw new BusinessRuleException("Quote must contain at least one item");
        }

        var quote = new QuoteModel
        {
            ClientId = request.ClientId,
            TemplateId = request.TemplateId,
            VatInclusive = request.VatInclusive,
            OrganizationId = organizationId,
            DateCreated = _timeProvider.GetUtcNow().UtcDateTime,
            ModifiedBy = modifiedBy,
            LastModifiedDate = _timeProvider.GetUtcNow().UtcDateTime,
            Items = request.Items.Select(item => new QuoteItem
            {
                Description = item.Description,
                Quantity = (int)item.Quantity,
                PricePerUnit = item.UnitPrice
            }).ToList()
        };

        var createdQuote = await _quoteRepository.AddAsync(quote);

        _logger.LogInformation("Quote {QuoteId} created by {User}", createdQuote.Id, modifiedBy);

        // Publish Kafka event for PDF generation
        try
        {
            await _kafkaProducer.PublishQuoteCreatedEventAsync(createdQuote.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish Kafka event for Quote {QuoteId}. Quote saved but PDF generation not triggered.",
                createdQuote.Id);
        }

        // Auto-create QuoteFirst workflow
        try
        {
            await _workflowService.CreateWorkflowAsync(new CreateWorkflowRequest
            {
                Type = WorkflowType.QuoteFirst,
                ClientId = request.ClientId,
                QuoteId = createdQuote.Id
            }, organizationId, modifiedBy);

            _logger.LogInformation("Workflow auto-created for Quote {QuoteId}", createdQuote.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to auto-create workflow for Quote {QuoteId}. Quote exists but workflow was not created.",
                createdQuote.Id);
        }

        return createdQuote.ToDto();
    }

    public async Task<QuoteResponse> UpdateQuoteAsync(int id, UpdateQuoteRequest request, string modifiedBy)
    {
        var existingQuote = await _quoteRepository.GetByIdWithDetailsAsync(id);

        if (existingQuote == null)
        {
            throw new NotFoundException("Quote", id);
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
            throw new BusinessRuleException("Quote must contain at least one item");
        }

        // Update properties
        existingQuote.ClientId = request.ClientId;
        existingQuote.NotificationSent = request.NotificationSent;
        existingQuote.TemplateId = request.TemplateId;
        existingQuote.VatInclusive = request.VatInclusive;
        existingQuote.LastModifiedDate = _timeProvider.GetUtcNow().UtcDateTime;
        existingQuote.ModifiedBy = modifiedBy;

        // Update items - clear and recreate
        existingQuote.Items.Clear();
        existingQuote.Items = request.Items.Select(item => new QuoteItem
        {
            QuoteId = id,
            Description = item.Description,
            Quantity = (int)item.Quantity,
            PricePerUnit = item.UnitPrice
        }).ToList();

        await _quoteRepository.UpdateAsync(existingQuote);

        _logger.LogInformation("Quote {QuoteId} updated by {User}", id, modifiedBy);

        // Re-publish the quote-created event so PdfGeneratorService regenerates the PDF with the new data
        try
        {
            await _kafkaProducer.PublishQuoteCreatedEventAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish PDF regeneration event for Quote {QuoteId}. Quote saved but PDF not regenerated.",
                id);
        }

        return existingQuote.ToDto();
    }

    public async Task DeleteQuoteAsync(int id)
    {
        var quote = await _quoteRepository.GetByIdWithDetailsAsync(id);

        if (quote == null)
        {
            throw new NotFoundException("Quote", id);
        }

        await _quoteRepository.DeleteAsync(quote);

        _logger.LogInformation("Quote {QuoteId} deleted", id);
    }

    public async Task<string?> GetQuotePdfUrlAsync(int id)
    {
        var quote = await _quoteRepository.GetByIdAsync(id);

        if (quote == null)
        {
            throw new NotFoundException("Quote", id);
        }

        if (string.IsNullOrEmpty(quote.PdfStorageKey))
        {
            throw new BusinessRuleException("PDF not yet generated for this quote");
        }

        return await _pdfStorageService.GetPresignedUrlAsync(quote.PdfStorageKey);
    }
}
