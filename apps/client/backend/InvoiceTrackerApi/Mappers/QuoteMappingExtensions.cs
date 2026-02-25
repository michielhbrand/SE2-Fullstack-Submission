using InvoiceTrackerApi.DTOs.Quote.Requests;
using InvoiceTrackerApi.DTOs.Quote.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between Quote domain models and DTOs
/// </summary>
public static class QuoteMappingExtensions
{
    public static QuoteResponse ToDto(this Quote quote)
    {
        return new QuoteResponse
        {
            Id = quote.Id,
            ClientId = quote.ClientId,
            Client = quote.Client?.ToDto(),
            DateCreated = quote.DateCreated,
            NotificationSent = quote.NotificationSent,
            LastModifiedDate = quote.LastModifiedDate,
            ModifiedBy = quote.ModifiedBy,
            PdfStorageKey = quote.PdfStorageKey,
            TemplateId = quote.TemplateId,
            VatInclusive = quote.VatInclusive,
            Items = quote.Items.Select(i => i.ToDto()).ToList()
        };
    }

    public static QuoteItemResponse ToDto(this QuoteItem item)
    {
        return new QuoteItemResponse
        {
            Id = item.Id,
            QuoteId = item.QuoteId,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.PricePerUnit,
            Total = item.TotalPrice
        };
    }

    public static Quote ToModel(this CreateQuoteRequest request)
    {
        return new Quote
        {
            ClientId = request.ClientId,
            TemplateId = request.TemplateId,
            Items = request.Items.Select(i => new QuoteItem
            {
                Description = i.Description,
                Quantity = i.Quantity,
                PricePerUnit = i.UnitPrice
            }).ToList()
        };
    }
}
