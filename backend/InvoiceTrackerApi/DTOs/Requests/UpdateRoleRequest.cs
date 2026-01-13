namespace InvoiceTrackerApi.DTOs.Requests;

/// <summary>
/// Request model for updating user role
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// Whether the user should have admin role
    /// </summary>
    public bool IsAdmin { get; set; }
}
