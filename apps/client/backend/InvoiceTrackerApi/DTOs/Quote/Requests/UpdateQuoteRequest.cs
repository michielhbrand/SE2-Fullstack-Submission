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

    [MaxLength(255)]
    public string? TemplateId { get; set; }

    [Required]
    public List<CreateQuoteItemRequest> Items { get; set; } = new();
}
