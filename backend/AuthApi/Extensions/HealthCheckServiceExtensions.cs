namespace AuthApi.Extensions;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }
}
