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
            .WithOpenApi()
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        LogoutRequest request,
        IKeycloakAuthService authService)
    {
        await authService.LogoutAsync(request.RefreshToken);
        return TypedResults.NoContent();
    }
}
