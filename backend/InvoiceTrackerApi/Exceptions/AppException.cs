namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Base exception class for all application-level exceptions.
/// This provides a common base for exception handling and ensures
/// all custom exceptions can be identified and handled consistently.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Gets the HTTP status code that should be returned for this exception.
    /// This is used by the global exception handler to map exceptions to HTTP responses.
    /// </summary>
    public abstract int StatusCode { get; }

    /// <summary>
    /// Gets the error type URI for RFC 9457 Problem Details.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Gets the error title for RFC 9457 Problem Details.
    /// </summary>
    public abstract string Title { get; }

    protected AppException(string message) : base(message)
    {
    }

    protected AppException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
