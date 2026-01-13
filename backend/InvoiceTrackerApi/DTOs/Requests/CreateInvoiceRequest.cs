using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Requests;

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
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
