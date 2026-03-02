namespace Shared.Core.Exceptions.Infrastructure;

public class InfrastructureException : AppException
{
    public override int StatusCode => 503;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.4";
    public override string Title => "Service Unavailable";

    public InfrastructureException(string message) : base(message) { }
    public InfrastructureException(string message, Exception innerException) : base(message, innerException) { }
}
