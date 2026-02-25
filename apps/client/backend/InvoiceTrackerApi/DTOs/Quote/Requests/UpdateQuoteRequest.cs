using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Quote.Requests;

/// <summary>
/// Request DTO for updating an existing quote
/// </summary>
public class UpdateQuoteRequest
{
    [Required]
    public int ClientId { get; set; }

    public bool NotificationSent { get; set; }

    public int? TemplateId { get; set; }

    /// <summary>
    /// When true, item prices include VAT. When false, VAT is calculated separately.
    /// </summary>
    public bool VatInclusive { get; set; } = true;

    [Required]
    public List<CreateQuoteItemRequest> Items { get; set; } = new();
}
