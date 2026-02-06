namespace InvoiceTrackerApi.DTOs.Organization.Requests;

/// <summary>
/// Request to add a member to an organization
/// </summary>
public class AddOrganizationMemberRequest
{
    /// <summary>
    /// Role of the user within the organization (e.g., "orgAdmin", "orgUser")
    /// </summary>
    public required string Role { get; set; }
}
