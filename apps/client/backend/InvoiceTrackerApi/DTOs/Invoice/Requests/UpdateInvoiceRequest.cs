using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Invoice.Requests;

/// <summary>
/// Request DTO for updating an existing invoice
/// </summary>
public class UpdateInvoiceRequest
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
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}
