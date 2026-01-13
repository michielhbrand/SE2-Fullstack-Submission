namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} with ID {key} not found")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
