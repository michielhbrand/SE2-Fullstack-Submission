using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ClientSurname { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ClientAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ClientCellphone { get; set; } = string.Empty;

    [Required]
    public DateTime DateCreated { get; set; }

    public bool NotificationSent { get; set; } = false;

    public DateTime? LastModifiedDate { get; set; }

    [MaxLength(255)]
    public string? ModifiedBy { get; set; }

    [MaxLength(500)]
    public string? PdfStorageKey { get; set; }

    [MaxLength(255)]
    public string? TemplateId { get; set; }

    // Navigation property for invoice items
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
