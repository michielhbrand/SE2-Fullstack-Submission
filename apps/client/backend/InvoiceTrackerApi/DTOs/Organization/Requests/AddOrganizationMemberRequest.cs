using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Organization.Requests;

/// <summary>
/// Request to add a member to an organization
/// </summary>
public class AddOrganizationMemberRequest
{
    /// <summary>
    /// Role of the user within the organization (e.g., "orgAdmin", "orgUser")
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    [MaxLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
    public required string Role { get; set; }
}
