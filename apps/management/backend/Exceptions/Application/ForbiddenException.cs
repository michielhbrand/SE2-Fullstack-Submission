namespace ManagementApi.Exceptions.Application;

/// <summary>
/// Exception thrown when a user lacks permission to access a resource.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(
            StatusCodes.Status403Forbidden,
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
            "Forbidden",
            message)
    {
    }
}
