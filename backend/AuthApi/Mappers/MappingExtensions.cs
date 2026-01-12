using AuthApi.Models;
using AuthApi.DTOs;

namespace AuthApi.Mappers;

/// <summary>
/// Extension methods for mapping between domain models and DTOs
/// </summary>
public static class MappingExtensions
{
    // Client mappings
    public static ClientDto ToDto(this Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Surname = client.Surname,
            Email = client.Email,
            Cellphone = client.Cellphone,
            Address = client.Address,
            Company = client.Company,
            DateCreated = client.DateCreated,
            LastModifiedDate = client.LastModifiedDate,
            ModifiedBy = client.ModifiedBy,
            KeycloakUserId = client.KeycloakUserId
        };
    }

    public static Client ToModel(this CreateClientRequest request)
    {
        return new Client
        {
            Name = request.Name,
            Surname = request.Surname,
            Email = request.Email,
            Cellphone = request.Cellphone,
            Address = request.Address,
            Company = request.Company,
            KeycloakUserId = request.KeycloakUserId
        };
    }

    // Invoice mappings
    public static InvoiceDto ToDto(this Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            ClientId = invoice.ClientId,
            Client = invoice.Client?.ToDto(),
            DateCreated = invoice.DateCreated,
            NotificationSent = invoice.NotificationSent,
            LastModifiedDate = invoice.LastModifiedDate,
            ModifiedBy = invoice.ModifiedBy,
            PdfStorageKey = invoice.PdfStorageKey,
            TemplateId = invoice.TemplateId,
            Items = invoice.Items.Select(i => i.ToDto()).ToList()
        };
    }

    public static InvoiceItemDto ToDto(this InvoiceItem item)
    {
        return new InvoiceItemDto
        {
            Id = item.Id,
            InvoiceId = item.InvoiceId,
            Description = item.Description,
            Quantity = item.Amount,
            UnitPrice = item.PricePerUnit,
            Total = item.TotalPrice
        };
    }

    public static Invoice ToModel(this CreateInvoiceRequest request)
    {
        return new Invoice
        {
            ClientId = request.ClientId,
            TemplateId = request.TemplateId,
            Items = request.Items.Select(i => new InvoiceItem
            {
                Description = i.Description,
                Amount = (int)i.Quantity,
                PricePerUnit = i.UnitPrice
            }).ToList()
        };
    }

    // Quote mappings
    public static QuoteDto ToDto(this Quote quote)
    {
        return new QuoteDto
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
            Items = quote.Items.Select(i => i.ToDto()).ToList()
        };
    }

    public static QuoteItemDto ToDto(this QuoteItem item)
    {
        return new QuoteItemDto
        {
            Id = item.Id,
            QuoteId = item.QuoteId,
            Description = item.Description,
            Quantity = item.Amount,
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
                Amount = (int)i.Quantity,
                PricePerUnit = i.UnitPrice
            }).ToList()
        };
    }

    // Template mappings
    public static TemplateDto ToDto(this Template template)
    {
        return new TemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Version = template.Version,
            StorageKey = template.StorageKey,
            Created = template.Created,
            CreatedBy = template.CreatedBy
        };
    }
}
