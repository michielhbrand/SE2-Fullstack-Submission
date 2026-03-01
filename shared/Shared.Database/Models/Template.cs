using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Database.Models;

public enum TemplateType
{
    Invoice,
    Quote
}

public class Template
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [Required]
    public int Version { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    public TemplateType Type { get; set; }

    // Null = system-level template visible to all organisations.
    // Non-null = template created by a specific organisation (future use).
    public int? OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }
}
