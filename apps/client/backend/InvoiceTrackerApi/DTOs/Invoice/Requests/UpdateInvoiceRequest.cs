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

    [Required]
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}
