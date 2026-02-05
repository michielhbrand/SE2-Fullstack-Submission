using FluentValidation;

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
            return Results.ValidationProblem(
                validationResult.ToDictionary(),
                title: "Validation Error",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        return await next(context);
    }
}
