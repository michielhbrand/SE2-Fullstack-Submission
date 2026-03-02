namespace Shared.Core.Exceptions.Infrastructure;

public class DatabaseUnavailableException : InfrastructureException
{
    public DatabaseUnavailableException(string message) : base(message) { }
    public DatabaseUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
