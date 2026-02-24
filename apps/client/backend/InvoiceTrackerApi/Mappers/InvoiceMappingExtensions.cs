using InvoiceTrackerApi.DTOs.Invoice.Requests;
using InvoiceTrackerApi.DTOs.Invoice.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between Invoice domain models and DTOs
/// </summary>
public static class InvoiceMappingExtensions
{
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
}
