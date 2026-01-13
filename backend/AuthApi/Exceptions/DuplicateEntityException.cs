namespace AuthApi.Exceptions;

/// <summary>
/// Exception thrown when a duplicate entity is detected
/// </summary>
public class DuplicateEntityException : BusinessRuleException
{
    public DuplicateEntityException(string message) : base(message)
    {
    }

    public DuplicateEntityException(string entityName, string fieldName, object value)
        : base($"A {entityName} with {fieldName} '{value}' already exists")
    {
    }
}
