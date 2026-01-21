namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when a user is authenticated but lacks permission to access a resource.
/// Maps to HTTP 403 Forbidden.
/// Use this when the user identity is known but they don't have the required permissions.
/// </summary>
public class ForbiddenException : AppException
{
    public override int StatusCode => 403;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4";
    public override string Title => "Forbidden";

    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
