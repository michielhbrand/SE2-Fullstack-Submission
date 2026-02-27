using InvoiceTrackerApi.DTOs.Client.Responses;

namespace InvoiceTrackerApi.DTOs.Invoice.Responses;

/// <summary>
/// Invoice data transfer object for API responses
/// </summary>
public class InvoiceResponse
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
    public bool VatInclusive { get; set; }
    public DateTime PayByDate { get; set; }
    /// <summary>
    /// Computed payment status: "Paid" | "Overdue" | "NotPaid"
    /// </summary>
    public string PaymentStatus { get; set; } = "NotPaid";
    public List<InvoiceItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Invoice item data transfer object
/// </summary>
public class InvoiceItemResponse
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
