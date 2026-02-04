using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ManagementApi.Exceptions;

/// <summary>
/// Global exception handler that maps exceptions to RFC 9457 Problem Details responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogException(exception);

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        if (exception is AppException appException)
        {
            return new ProblemDetails
            {
                Status = appException.StatusCode,
                Type = appException.Type,
                Title = appException.Title,
                Detail = appException.Message,
                Instance = httpContext.Request.Path
            };
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred while processing your request.",
            Instance = httpContext.Request.Path
        };
    }

    private void LogException(Exception exception)
    {
        if (exception is AppException)
        {
            _logger.LogWarning(
                exception,
                "Application exception occurred: {ExceptionType} - {Message}",
                exception.GetType().Name,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unexpected exception occurred: {ExceptionType} - {Message}",
                exception.GetType().Name,
                exception.Message);
        }
    }
}
