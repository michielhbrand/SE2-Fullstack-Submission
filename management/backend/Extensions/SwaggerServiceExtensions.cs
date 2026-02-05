using NSwag;
using NSwag.Generation.Processors.Security;

namespace ManagementApi.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(config =>
        {
            config.DocumentName = "v1";
            config.Title = "Management API";
            config.Version = "v1";
            config.Description = "API for managing organizations and system administration";
            
            config.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
            });

            config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
            
            // Generate detailed schemas for better TypeScript types
            config.SchemaSettings.GenerateAbstractSchemas = false;
            config.SchemaSettings.GenerateExamples = true;
        });

        return services;
    }
}
