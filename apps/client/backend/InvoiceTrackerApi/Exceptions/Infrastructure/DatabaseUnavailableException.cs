namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when the database is unavailable or unreachable.
/// Maps to HTTP 503 Service Unavailable.
/// Use this specifically for database connectivity and availability issues.
/// </summary>
public class DatabaseUnavailableException : InfrastructureException
{
    public DatabaseUnavailableException(string message) : base(message)
    {
    }

    public DatabaseUnavailableException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
