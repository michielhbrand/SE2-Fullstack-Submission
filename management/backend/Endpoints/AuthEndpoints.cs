using ManagementApi.DTOs.Auth;
using ManagementApi.Services.Auth;

namespace ManagementApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // POST /api/auth/admin-login - System Admin login
        group.MapPost("/admin-login", AdminLogin)
            .WithName("AdminLogin")
            .WithOpenApi()
            .AllowAnonymous();

        // POST /api/auth/refresh - Refresh access token
        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithOpenApi()
            .AllowAnonymous();

        // POST /api/auth/logout - Logout
        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithOpenApi()
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> AdminLogin(
        LoginRequest request,
        IKeycloakAuthService authService)
    {
        var result = await authService.LoginAsync(request.Username, request.Password);

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = result.TokenType,
            Roles = result.Roles
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> RefreshToken(
        RefreshTokenRequest request,
        IKeycloakAuthService authService)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);

        var response = new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = result.TokenType,
            Roles = result.Roles
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> Logout(
        LogoutRequest request,
        IKeycloakAuthService authService)
    {
        await authService.LogoutAsync(request.RefreshToken);
        return Results.NoContent();
    }
}
