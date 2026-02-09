using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerApi.Extensions;

/// <summary>
/// Extension methods for configuring validation services
/// </summary>
public static class ValidationServiceExtensions
{
    /// <summary>
    /// Configures model validation to return RFC 9457 Problem Details responses
    /// </summary>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                    Title = "Validation Error",
                    Detail = "One or more validation errors occurred. Please check your input.",
                    Instance = context.HttpContext.Request.Path
                };

                // Add validation errors as extensions
                problemDetails.Extensions["errors"] = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                    );

                return new BadRequestObjectResult(problemDetails);
            };
        });

        return services;
    }
}
