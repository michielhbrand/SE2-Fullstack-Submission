using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Database.Models;

public class Quote
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
    
    /// <summary>
    /// When true, item prices include VAT (document shows only total).
    /// When false, item prices exclude VAT (document shows subtotal, VAT amount, and total).
    /// </summary>
    public bool VatInclusive { get; set; } = true;
    
    public DateTime? LastModifiedDate { get; set; }
    
    [MaxLength(255)]
    public string? ModifiedBy { get; set; }
    
    [MaxLength(500)]
    public string? PdfStorageKey { get; set; }
    
    public int? TemplateId { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public Template? Template { get; set; }

    [Required]
    public int OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }
    
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
