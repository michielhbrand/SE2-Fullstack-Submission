namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when user authentication fails.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : AppException
{
    public override int StatusCode => 401;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2";
    public override string Title => "Unauthorized";

    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
