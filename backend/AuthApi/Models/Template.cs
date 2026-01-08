using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Models;

[Index(nameof(Name), nameof(Version), IsUnique = true)]
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
}
