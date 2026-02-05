namespace ManagementApi.DTOs.Organization;

/// <summary>
/// Query parameters for filtering, searching, and sorting organizations
/// </summary>
public class GetOrganizationsQuery
{
    /// <summary>
    /// Search term to filter organizations by name, email, phone, registration number, city, or country
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by active status: "all", "active", or "inactive"
    /// </summary>
    public string? Status { get; set; } = "all";

    /// <summary>
    /// Column to sort by: "name", "email", "city", "status", or "created"
    /// </summary>
    public string? SortBy { get; set; } = "name";

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
}
