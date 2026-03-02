using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Core.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogException(exception);

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

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

        var detail = _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred while processing your request.";

        var internalError = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            Title = "Internal Server Error",
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        internalError.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (_environment.IsDevelopment())
        {
            internalError.Extensions["stackTrace"] = exception.StackTrace;
            internalError.Extensions["exceptionType"] = exception.GetType().FullName;
        }

        return internalError;
    }

    private void LogException(Exception exception)
    {
        if (exception is AppException appException)
        {
            _logger.LogWarning(
                exception,
                "Application exception occurred: {ExceptionType} - Status: {StatusCode} - {Message}",
                exception.GetType().Name,
                appException.StatusCode,
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
