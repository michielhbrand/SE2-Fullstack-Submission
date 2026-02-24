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
}
