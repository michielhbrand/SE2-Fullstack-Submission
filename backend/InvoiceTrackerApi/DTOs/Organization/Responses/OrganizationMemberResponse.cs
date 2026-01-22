namespace InvoiceTrackerApi.DTOs.Organization.Responses;

/// <summary>
/// Response DTO for organization member information
/// </summary>
public class OrganizationMemberResponse
{
    /// <summary>
    /// Organization ID
    /// </summary>
    public int OrganizationId { get; set; }
    
    /// <summary>
    /// Keycloak user ID (sub claim from JWT)
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// Role of the user within the organization
    /// </summary>
    public required string Role { get; set; }
}
