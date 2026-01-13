using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.DTOs.Responses;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Quote;
using InvoiceTrackerApi.Services.PdfStorage;
using QuoteModel = InvoiceTrackerApi.Models.Quote;

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
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        IQuoteRepository quoteRepository,
        IClientRepository clientRepository,
        IKafkaProducerService kafkaProducer,
        IPdfStorageService pdfStorageService,
        ILogger<QuoteService> logger)
    {
        _quoteRepository = quoteRepository;
        _clientRepository = clientRepository;
        _kafkaProducer = kafkaProducer;
        _pdfStorageService = pdfStorageService;
        _logger = logger;
    }

    public async Task<PaginatedResponse<QuoteResponse>> GetQuotesAsync(int page, int pageSize)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var quotes = await _quoteRepository.GetAllAsync(page, pageSize);
        var totalCount = await _quoteRepository.GetTotalCountAsync();
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

    public async Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, string modifiedBy)
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
            DateCreated = DateTime.UtcNow,
            ModifiedBy = modifiedBy,
            LastModifiedDate = DateTime.UtcNow,
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
            _logger.LogError(ex, "Failed to publish Kafka event for Quote {QuoteId}. Rolling back quote creation.", createdQuote.Id);
            
            // Rollback: Delete the created quote to maintain data consistency
            try
            {
                await _quoteRepository.DeleteAsync(createdQuote);
                _logger.LogInformation("Successfully rolled back Quote {QuoteId}", createdQuote.Id);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback Quote {QuoteId} after Kafka publish failure", createdQuote.Id);
            }
            
            // Throw exception to inform the API client
            throw new BusinessRuleException("Failed to create quote: Unable to trigger PDF generation. Please try again.", ex);
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
        existingQuote.LastModifiedDate = DateTime.UtcNow;
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
