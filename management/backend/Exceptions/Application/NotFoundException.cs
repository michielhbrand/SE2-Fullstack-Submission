namespace ManagementApi.Exceptions.Application;

public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base(
            StatusCodes.Status404NotFound,
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
            "Resource Not Found",
            $"{resourceName} with identifier '{key}' was not found.")
    {
    }

    public NotFoundException(string message)
        : base(
            StatusCodes.Status404NotFound,
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
            "Resource Not Found",
            message)
    {
    }
}
