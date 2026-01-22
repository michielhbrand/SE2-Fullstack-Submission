namespace InvoiceTrackerApi.DTOs.Organization.Requests;

/// <summary>
/// Request to update a member's role within an organization
/// </summary>
public class UpdateMemberRoleRequest
{
    /// <summary>
    /// New role for the user (e.g., "orgAdmin", "orgUser")
    /// </summary>
    public required string Role { get; set; }
}
