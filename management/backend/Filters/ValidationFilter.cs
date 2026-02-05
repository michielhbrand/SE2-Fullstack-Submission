using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ManagementApi.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T>? _validator;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _validator = serviceProvider.GetService<IValidator<T>>();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_validator == null)
        {
            return await next(context);
        }

        var requestObject = context.Arguments.OfType<T>().FirstOrDefault();
        
        if (requestObject == null)
        {
            return await next(context);
        }

        var cancellationToken = context.HttpContext.RequestAborted;
        var validationResult = await _validator.ValidateAsync(requestObject, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            // Create proper RFC 9457 ProblemDetails for validation errors
            var problemDetails = new ValidationProblemDetails(validationResult.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                Title = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path
            };

            return Results.ValidationProblem(
                problemDetails.Errors,
                detail: problemDetails.Detail,
                instance: problemDetails.Instance,
                title: problemDetails.Title,
                type: problemDetails.Type,
                statusCode: problemDetails.Status
            );
        }

        return await next(context);
    }
}
