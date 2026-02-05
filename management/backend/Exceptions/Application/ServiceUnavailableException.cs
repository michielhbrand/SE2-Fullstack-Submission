namespace ManagementApi.Exceptions.Application;

/// <summary>
/// Exception thrown when an external service is unavailable.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public class ServiceUnavailableException : AppException
{
    public ServiceUnavailableException(string message, Exception? innerException = null)
        : base(
            StatusCodes.Status503ServiceUnavailable,
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.4",
            "Service Unavailable",
            message)
    {
    }
}
