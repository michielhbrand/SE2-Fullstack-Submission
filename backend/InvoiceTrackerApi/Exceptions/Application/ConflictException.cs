namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with the current state of the resource.
/// Maps to HTTP 409 Conflict.
/// Use this for general conflict scenarios beyond duplicate entities.
/// </summary>
public class ConflictException : AppException
{
    public override int StatusCode => 409;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10";
    public override string Title => "Conflict";

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
