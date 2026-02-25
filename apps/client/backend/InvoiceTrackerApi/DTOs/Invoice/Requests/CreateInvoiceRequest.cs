using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Invoice.Requests;

/// <summary>
/// Request DTO for creating a new invoice
/// </summary>
public class CreateInvoiceRequest
{
    [Required]
    public int ClientId { get; set; }

    public int? TemplateId { get; set; }

    /// <summary>
    /// When true, item prices include VAT (document shows only total).
    /// When false, item prices exclude VAT (document shows subtotal, VAT amount, and total).
    /// </summary>
    public bool VatInclusive { get; set; } = true;

    /// <summary>
    /// Number of days from today until the invoice is due. Defaults to 30 if not specified.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Pay by days must be between 1 and 365")]
    public int PayByDays { get; set; } = 30;

    [Required]
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Request DTO for creating an invoice item
/// </summary>
public class CreateInvoiceItemRequest
{
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }
}
