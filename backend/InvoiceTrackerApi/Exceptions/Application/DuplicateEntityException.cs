namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Exception thrown when a duplicate entity is detected.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class DuplicateEntityException : AppException
{
    public override int StatusCode => 409;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10";
    public override string Title => "Duplicate Entity";

    public DuplicateEntityException(string message) : base(message)
    {
    }

    public DuplicateEntityException(string entityName, string fieldName, object value)
        : base($"A {entityName} with {fieldName} '{value}' already exists")
    {
    }
}
