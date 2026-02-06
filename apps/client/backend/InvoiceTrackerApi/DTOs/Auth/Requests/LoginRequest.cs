namespace InvoiceTrackerApi.DTOs.Auth.Requests;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
