using ManagementApi.DTOs.Auth;
using ManagementApi.Filters;
using ManagementApi.Mappers;
using ManagementApi.Services.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Auth;

public static class RefreshTokenEndpoint
{
    public static RouteHandlerBuilder MapRefreshToken(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/refresh", Handle)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Generates a new access token using a valid refresh token")
            .WithOpenApi()
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
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
