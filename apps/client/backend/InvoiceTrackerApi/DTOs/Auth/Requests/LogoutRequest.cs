using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerApi.DTOs.Auth.Requests;

/// <summary>
/// Request model for user logout
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// Refresh token to invalidate
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
