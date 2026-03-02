namespace Shared.Core.Exceptions.Application;

public class ForbiddenException : AppException
{
    public override int StatusCode => 403;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4";
    public override string Title => "Forbidden";

    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}
