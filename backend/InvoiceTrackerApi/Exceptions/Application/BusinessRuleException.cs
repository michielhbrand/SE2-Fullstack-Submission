namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class BusinessRuleException : AppException
{
    public override int StatusCode => 400;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1";
    public override string Title => "Business Rule Violation";

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
