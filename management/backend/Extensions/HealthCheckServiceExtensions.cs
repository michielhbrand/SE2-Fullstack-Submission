using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ManagementApi.Extensions;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString!,
                name: "database",
                tags: new[] { "db", "postgresql" });

        return services;
    }
}
