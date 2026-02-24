using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Invoice.Requests;

/// <summary>
/// Request DTO for converting a quote into an invoice
/// </summary>
public class ConvertQuoteToInvoiceRequest
{
    /// <summary>
    /// The ID of the quote to convert
    /// </summary>
    [Required]
    public int QuoteId { get; set; }

    /// <summary>
    /// Optional template ID for the invoice PDF
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Number of days from today until the invoice is due. Defaults to 30 if not specified.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Pay by days must be between 1 and 365")]
    public int PayByDays { get; set; } = 30;
}
