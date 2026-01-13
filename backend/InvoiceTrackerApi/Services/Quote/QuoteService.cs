using InvoiceTrackerApi.DTOs.Requests;
using InvoiceTrackerApi.DTOs.Responses;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Models;
using InvoiceTrackerApi.Repositories.Client;
using InvoiceTrackerApi.Repositories.Quote;
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
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        IQuoteRepository quoteRepository,
        IClientRepository clientRepository,
        IKafkaProducerService kafkaProducer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<QuoteService> logger)
    {
        _quoteRepository = quoteRepository;
        _clientRepository = clientRepository;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PaginatedResponse<QuoteModel>> GetQuotesAsync(int page, int pageSize)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var quotes = await _quoteRepository.GetAllAsync(page, pageSize);
        var totalCount = await _quoteRepository.GetTotalCountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<QuoteModel>
        {
            Data = quotes.ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<QuoteModel> GetQuoteByIdAsync(int id)
    {
        var quote = await _quoteRepository.GetByIdWithDetailsAsync(id);

        if (quote == null)
        {
            throw new NotFoundException("Quote", id);
        }

        return quote;
    }

    public async Task<QuoteModel> CreateQuoteAsync(CreateQuoteRequest request, string modifiedBy)
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
                Amount = (int)item.Quantity,
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
            _logger.LogError(ex, "Failed to publish Kafka event for Quote {QuoteId}", createdQuote.Id);
            // Continue - quote is created, PDF generation will be retried
        }

        return createdQuote;
    }

    public async Task<QuoteModel> UpdateQuoteAsync(int id, UpdateQuoteRequest request, string modifiedBy)
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
            Amount = (int)item.Quantity,
            PricePerUnit = item.UnitPrice
        }).ToList();

        await _quoteRepository.UpdateAsync(existingQuote);

        _logger.LogInformation("Quote {QuoteId} updated by {User}", id, modifiedBy);

        return existingQuote;
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

        try
        {
            var pdfServiceUrl = _configuration["PdfGeneratorService:Url"] ?? "http://localhost:5001";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{pdfServiceUrl}/api/Pdf/presigned-url?storageKey={Uri.EscapeDataString(quote.PdfStorageKey)}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return result?["url"];
            }

            _logger.LogWarning("Failed to get presigned URL from PDF Generator Service. Status: {Status}", response.StatusCode);
            throw new BusinessRuleException("Failed to generate PDF URL");
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogError(ex, "Error getting PDF URL for Quote {QuoteId}", id);
            throw new BusinessRuleException("Error generating PDF URL");
        }
    }
}
