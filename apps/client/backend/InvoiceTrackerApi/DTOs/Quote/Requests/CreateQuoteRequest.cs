using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Quote.Requests;

/// <summary>
/// Request DTO for creating a new quote
/// </summary>
public class CreateQuoteRequest
{
    [Required]
    public int ClientId { get; set; }

    [MaxLength(255)]
    public string? TemplateId { get; set; }

    [Required]
    public List<CreateQuoteItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Request DTO for creating a quote item
/// </summary>
public class CreateQuoteItemRequest
{
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
