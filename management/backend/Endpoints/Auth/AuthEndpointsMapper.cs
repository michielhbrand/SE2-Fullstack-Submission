using ManagementApi.DTOs.Auth;
using ManagementApi.Services.Auth;

namespace ManagementApi.Endpoints.Auth;

public static class AuthEndpointsMapper
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapAdminLogin();
        group.MapRefreshToken();
        group.MapLogout();

        return app;
    }

    public static LoginResponse ToLoginResponse(this TokenResponse result)
    {
        return new()
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = result.TokenType,
            Roles = result.Roles
        };
    }
}
