using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceTrackerApi.Models;

public class Quote
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public DateTime DateCreated { get; set; }
    public bool NotificationSent { get; set; } = false;
    public DateTime? LastModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? PdfStorageKey { get; set; }
    public string? TemplateId { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
