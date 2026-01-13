namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Base exception for business rule violations
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
