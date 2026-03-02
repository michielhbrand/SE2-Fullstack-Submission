namespace Shared.Core.Exceptions.Application;

public class ServiceUnavailableException : AppException
{
    public override int StatusCode => 503;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.4";
    public override string Title => "Service Unavailable";

    public ServiceUnavailableException(string message) : base(message) { }
    public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
