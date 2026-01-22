using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.DTOs.Client.Responses;
using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;
using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using InvoiceTrackerApi.DTOs.Quote.Requests;
using InvoiceTrackerApi.DTOs.Quote.Responses;
using InvoiceTrackerApi.DTOs.Template.Responses;
using InvoiceTrackerApi.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between domain models and DTOs
/// </summary>
public static class MappingExtensions
{
    // Client mappings
    public static ClientResponse ToDto(this Client client)
    {
        return new ClientResponse
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
    public static InvoiceResponse ToDto(this Invoice invoice)
    {
        return new InvoiceResponse
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

    public static InvoiceItemResponse ToDto(this InvoiceItem item)
    {
        return new InvoiceItemResponse
        {
            Id = item.Id,
            InvoiceId = item.InvoiceId,
            Description = item.Description,
            Quantity = item.Quantity,
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
                Quantity = i.Quantity,
                PricePerUnit = i.UnitPrice
            }).ToList()
        };
    }

    // Quote mappings
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

    // Template mappings
    public static TemplateResponse ToDto(this Template template)
    {
        return new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Version = template.Version,
            StorageKey = template.StorageKey,
            Created = template.Created,
            CreatedBy = template.CreatedBy
        };
    }

    // Organization mappings
    public static OrganizationResponse ToDto(this Models.Organization organization)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Address = organization.Address?.ToDto() ?? new AddressResponse
            {
                Id = 0,
                FirstLine = "",
                City = "",
                Code = ""
            },
            UserIds = organization.UserIds
        };
    }

    public static Models.Organization ToModel(this CreateOrganizationRequest request)
    {
        return new Models.Organization
        {
            Name = request.Name,
            UserIds = request.UserIds
        };
    }

    // Address mappings
    public static AddressResponse ToDto(this Address address)
    {
        return new AddressResponse
        {
            Id = address.Id,
            FirstLine = address.FirstLine,
            SecondLine = address.SecondLine,
            City = address.City,
            Code = address.Code
        };
    }

    public static Address ToModel(this CreateAddressRequest request)
    {
        return new Address
        {
            FirstLine = request.FirstLine,
            SecondLine = request.SecondLine,
            City = request.City,
            Code = request.Code
        };
    }

    // BankAccount mappings
    public static BankAccountResponse ToDto(this BankAccount bankAccount)
    {
        return new BankAccountResponse
        {
            Id = bankAccount.Id,
            BankName = bankAccount.BankName,
            BranchCode = bankAccount.BranchCode,
            AccountNumber = bankAccount.AccountNumber,
            AccountType = bankAccount.AccountType,
            Active = bankAccount.Active
        };
    }

    public static BankAccount ToModel(this CreateBankAccountRequest request, int organizationId)
    {
        return new BankAccount
        {
            BankName = request.BankName,
            BranchCode = request.BranchCode,
            AccountNumber = request.AccountNumber,
            AccountType = request.AccountType,
            Active = request.Active,
            OrganizationId = organizationId
        };
    }
}
