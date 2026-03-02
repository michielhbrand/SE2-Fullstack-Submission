namespace Shared.Core.Exceptions.Application;

public class ValidationException : AppException
{
    public override int StatusCode => 400;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1";
    public override string Title => "Validation Failed";

    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
