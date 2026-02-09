using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

/// <summary>
/// Request to update a member's role within an organization
/// </summary>
public class UpdateMemberRoleRequest
{
    /// <summary>
    /// New role for the user (e.g., "orgAdmin", "orgUser")
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    [MaxLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
    public required string Role { get; set; }
}
