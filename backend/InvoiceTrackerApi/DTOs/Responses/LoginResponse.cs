namespace InvoiceTrackerApi.DTOs.Responses;

/// <summary>
/// Response containing authentication tokens
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public required string Access_token { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public required string Refresh_token { get; set; }

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public required int Expires_in { get; set; }

    /// <summary>
    /// Token type (typically "Bearer")
    /// </summary>
    public required string Token_type { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string>? Roles { get; set; }
}
