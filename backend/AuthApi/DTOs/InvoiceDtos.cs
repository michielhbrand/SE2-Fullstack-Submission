using System.ComponentModel.DataAnnotations;

namespace AuthApi.DTOs;

/// <summary>
/// Invoice data transfer object for API responses
/// </summary>
public class InvoiceDto
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
    public List<InvoiceItemDto> Items { get; set; } = new();
}

/// <summary>
/// Invoice item data transfer object
/// </summary>
public class InvoiceItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Request DTO for creating a new invoice
/// </summary>
public class CreateInvoiceRequest
{
    [Required]
    public int ClientId { get; set; }

    [MaxLength(255)]
    public string? TemplateId { get; set; }

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
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Request DTO for updating an existing invoice
/// </summary>
public class UpdateInvoiceRequest
{
    [Required]
    public int ClientId { get; set; }

    public bool NotificationSent { get; set; }

    [MaxLength(255)]
    public string? TemplateId { get; set; }

    [Required]
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Response DTO for PDF URL
/// </summary>
public class PdfUrlResponse
{
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for template list
/// </summary>
public class TemplateListResponse
{
    public List<string> Templates { get; set; } = new();
}
