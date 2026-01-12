using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

/// <summary>
/// Quote data transfer object for API responses
/// </summary>
public class QuoteDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public ClientDto? Client { get; set; }
    public DateTime DateCreated { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? PdfStorageKey { get; set; }
    public string? TemplateId { get; set; }
    public List<QuoteItemDto> Items { get; set; } = new();
}

/// <summary>
/// Quote item data transfer object
/// </summary>
public class QuoteItemDto
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

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
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

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
