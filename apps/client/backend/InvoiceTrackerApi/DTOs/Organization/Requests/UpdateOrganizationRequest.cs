using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class UpdateOrganizationRequest
{
    [MaxLength(200, ErrorMessage = "Organization name cannot exceed 200 characters")]
    public string? Name { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Address ID must be a positive number")]
    public int? AddressId { get; set; }
    
    public List<int>? BankAccountIds { get; set; }
}
