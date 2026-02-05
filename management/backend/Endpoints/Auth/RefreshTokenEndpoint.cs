using ManagementApi.DTOs.Auth;
using ManagementApi.Filters;
using ManagementApi.Services.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Auth;

public static class RefreshTokenEndpoint
{
    public static RouteHandlerBuilder MapRefreshToken(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/refresh", Handle)
            .WithName("RefreshToken")
            .WithOpenApi()
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>();
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Handle(
        RefreshTokenRequest request,
        IKeycloakAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        var response = result.ToLoginResponse();

        return TypedResults.Ok(response);
    }
}
