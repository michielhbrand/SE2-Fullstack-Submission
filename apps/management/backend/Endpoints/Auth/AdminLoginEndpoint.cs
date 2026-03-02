using ManagementApi.DTOs.Auth;
using ManagementApi.Filters;
using ManagementApi.Mappers;
using ManagementApi.Services.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Auth;

public static class AdminLoginEndpoint
{
    public static RouteHandlerBuilder MapAdminLogin(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/admin-login", Handle)
            .WithName("AdminLogin")
            .WithSummary("Admin login")
            .WithDescription("Authenticates an admin user and returns access and refresh tokens")
            .WithOpenApi()
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Handle(
        LoginRequest request,
        IKeycloakTokenService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Username, request.Password, cancellationToken);

        var response = result.ToLoginResponse();

        return TypedResults.Ok(response);
    }
}
