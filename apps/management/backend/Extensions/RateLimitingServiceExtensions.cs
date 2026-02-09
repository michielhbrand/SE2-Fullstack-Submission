using AspNetCoreRateLimit;

namespace ManagementApi.Extensions;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddRateLimitingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Load configuration from appsettings.json
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
        services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
        services.Configure<ClientRateLimitPolicies>(configuration.GetSection("ClientRateLimitPolicies"));

        // Add memory cache for rate limit counters
        services.AddMemoryCache();

        // Add rate limit configuration
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();

        // Add rate limit configuration
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        // Add processing strategy
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

        return services;
    }

    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        // Use IP rate limiting middleware
        app.UseIpRateLimiting();

        // Optionally use client rate limiting middleware
        // app.UseClientRateLimiting();

        return app;
    }
}
