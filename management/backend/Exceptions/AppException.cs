namespace ManagementApi.Exceptions;

/// <summary>
/// Base class for all application-specific exceptions.
/// Provides a consistent structure for error handling across the API.
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Type { get; }
    public string Title { get; }

    protected AppException(
        int statusCode,
        string type,
        string title,
        string message) : base(message)
    {
        StatusCode = statusCode;
        Type = type;
        Title = title;
    }
}
