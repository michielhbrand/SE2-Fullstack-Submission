using InvoiceTrackerApi.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Exceptions;

/// <summary>
/// Global exception handler that maps exceptions to RFC 9457 Problem Details responses.
/// This handler centralizes all exception handling logic, ensuring consistent error responses
/// across the entire API without requiring try/catch blocks in controllers.
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

    /// <summary>
    /// Creates an RFC 9457 compliant ProblemDetails object from an exception.
    /// </summary>
    private ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        if (exception is AppException appException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = appException.StatusCode,
                Type = appException.Type,
                Title = appException.Title,
                Detail = appException.Message,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            return problemDetails;
        }

        var internalError = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred while processing your request.",
            Instance = httpContext.Request.Path
        };
        internalError.Extensions["traceId"] = httpContext.TraceIdentifier;
        return internalError;
    }

    /// <summary>
    /// Logs exceptions with appropriate severity levels.
    /// Application exceptions are logged as warnings, unexpected exceptions as errors.
    /// </summary>
    private void LogException(Exception exception)
    {
        if (exception is AppException appException)
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
                "Unexpected exception occurred: {ExceptionType} - {Message}\nStackTrace: {StackTrace}",
                exception.GetType().Name,
                exception.Message,
                exception.StackTrace);
        }
    }
}
