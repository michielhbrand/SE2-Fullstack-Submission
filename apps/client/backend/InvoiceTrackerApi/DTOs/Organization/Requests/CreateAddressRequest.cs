using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

public class CreateAddressRequest
{
    [Required(ErrorMessage = "First line is required")]
    [MaxLength(200, ErrorMessage = "First line cannot exceed 200 characters")]
    public required string FirstLine { get; set; }
    
    [MaxLength(200, ErrorMessage = "Second line cannot exceed 200 characters")]
    public string? SecondLine { get; set; }
    
    [Required(ErrorMessage = "City is required")]
    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public required string City { get; set; }
    
    [Required(ErrorMessage = "Code is required")]
    [MaxLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
    public required string Code { get; set; }
}
