using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Requests;

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Username for the new user
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Email address for the new user
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// First name of the new user
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name of the new user
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Initial password for the new user
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Role to assign to the new user (orgUser or orgAdmin only)
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}
