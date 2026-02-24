using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }

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

    [Required]
    public int OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
