namespace ManagementApi.Exceptions.Application;

public class ValidationException : AppException
{
    public ValidationException(string message)
        : base(
            StatusCodes.Status400BadRequest,
            "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
            "Validation Error",
            message)
    {
    }
}
