namespace Shared.Core.Exceptions.Application;

public class NotFoundException : AppException
{
    public override int StatusCode => 404;
    public override string Type => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5";
    public override string Title => "Resource Not Found";

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with ID {key} not found") { }
    public NotFoundException(string message) : base(message) { }
}
