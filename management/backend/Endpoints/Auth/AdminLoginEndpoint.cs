using ManagementApi.DTOs.Auth;
using ManagementApi.Filters;
using ManagementApi.Services.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Auth;

public static class AdminLoginEndpoint
{
    public static RouteHandlerBuilder MapAdminLogin(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/admin-login", Handle)
            .WithName("AdminLogin")
            .WithOpenApi()
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Handle(
        LoginRequest request,
        IKeycloakAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Username, request.Password, cancellationToken);

        var response = result.ToLoginResponse();

        return TypedResults.Ok(response);
    }
}
