using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateAddressRequest
{
    [Required(ErrorMessage = "Street is required")]
    [MaxLength(255, ErrorMessage = "Street cannot exceed 255 characters")]
    public required string Street { get; set; }
    
    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string? City { get; set; }
    
    [MaxLength(100, ErrorMessage = "State cannot exceed 100 characters")]
    public string? State { get; set; }
    
    [MaxLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string? PostalCode { get; set; }
    
    [MaxLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
    public string? Country { get; set; }
}
