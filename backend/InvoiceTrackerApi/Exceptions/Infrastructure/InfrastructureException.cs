namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when an infrastructure-level failure occurs.
/// Maps to HTTP 503 Service Unavailable.
/// Use this to wrap external service failures, database connectivity issues, etc.
/// </summary>
public class InfrastructureException : AppException
{
    public override int StatusCode => 503;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.4";
    public override string Title => "Service Unavailable";

    public InfrastructureException(string message) : base(message)
    {
    }

    public InfrastructureException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
