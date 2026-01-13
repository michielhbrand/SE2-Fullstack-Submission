using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceTrackerApi.Models;

public class Quote
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }
    
    public DateTime DateCreated { get; set; }
    
    public bool NotificationSent { get; set; } = false;
    
    public DateTime? LastModifiedDate { get; set; }
    
    [MaxLength(255)]
    public string? ModifiedBy { get; set; }
    
    [MaxLength(500)]
    public string? PdfStorageKey { get; set; }
    
    [MaxLength(255)]
    public string? TemplateId { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
