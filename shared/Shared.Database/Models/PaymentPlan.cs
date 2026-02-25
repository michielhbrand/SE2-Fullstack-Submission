using System.ComponentModel.DataAnnotations;

namespace Shared.Database.Models;

public class PaymentPlan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Maximum number of users allowed in an organization on this plan.
    /// -1 means unlimited.
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// Monthly cost in South African Rand.
    /// </summary>
    public decimal MonthlyCostRand { get; set; }
}
