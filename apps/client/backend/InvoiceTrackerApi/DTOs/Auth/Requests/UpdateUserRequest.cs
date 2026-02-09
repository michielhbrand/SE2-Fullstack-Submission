using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using InvoiceTrackerApi.Services.Auth;

namespace InvoiceTrackerApi.DTOs.Auth.Requests;

/// <summary>
/// Request model for updating user details
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; set; }

    [Required]
    public bool Active { get; set; }
}
