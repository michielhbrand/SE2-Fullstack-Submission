using ManagementApi.DTOs.Auth;
using ManagementApi.Services.Auth;

namespace ManagementApi.Endpoints.Auth;

public static class AuthEndpointsConfiguration
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapAdminLogin();
        group.MapRefreshToken();
        group.MapLogout();

        return app;
    }
}
