using ManagementApi.DTOs.Auth;
using ManagementApi.Filters;
using ManagementApi.Services.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Auth;

public static class LogoutEndpoint
{
    public static RouteHandlerBuilder MapLogout(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/logout", Handle)
            .WithName("Logout")
            .WithSummary("User logout")
            .WithDescription("Logs out a user by invalidating their refresh token")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        LogoutRequest request,
        IKeycloakTokenService authService,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return TypedResults.NoContent();
    }
}
