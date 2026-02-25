using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class UpdateOrganizationRequest
{
    [MaxLength(200, ErrorMessage = "Organization name cannot exceed 200 characters")]
    public string? Name { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Address ID must be a positive number")]
    public int? AddressId { get; set; }

    // Inline address fields — when provided, update the organization's existing address record
    [MaxLength(300)]
    public string? Street { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// VAT percentage rate (e.g. 15 means 15%). Must be between 0 and 100.
    /// </summary>
    [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
    public decimal? VatRate { get; set; }

    public List<int>? BankAccountIds { get; set; }
}
