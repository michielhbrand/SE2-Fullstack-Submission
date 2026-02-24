using InvoiceTrackerApi.DTOs.Client.Responses;

namespace InvoiceTrackerApi.DTOs.Quote.Responses;

/// <summary>
/// Quote data transfer object for API responses
/// </summary>
public class QuoteResponse
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public ClientResponse? Client { get; set; }
    public DateTime DateCreated { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? PdfStorageKey { get; set; }
    public int? TemplateId { get; set; }
    public List<QuoteItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Quote item data transfer object
/// </summary>
public class QuoteItemResponse
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
