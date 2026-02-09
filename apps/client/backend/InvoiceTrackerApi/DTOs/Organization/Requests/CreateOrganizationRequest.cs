using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateOrganizationRequest
{
    [Required(ErrorMessage = "Organization name is required")]
    [MaxLength(200, ErrorMessage = "Organization name cannot exceed 200 characters")]
    public required string Name { get; set; }
    
    [Required(ErrorMessage = "Address is required")]
    public required CreateAddressRequest Address { get; set; }
}
