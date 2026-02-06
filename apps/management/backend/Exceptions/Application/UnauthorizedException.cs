namespace ManagementApi.Exceptions.Application;

/// <summary>
/// Exception thrown when authentication fails or credentials are invalid.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(
            StatusCodes.Status401Unauthorized,
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
            "Unauthorized",
            message)
    {
    }
}
