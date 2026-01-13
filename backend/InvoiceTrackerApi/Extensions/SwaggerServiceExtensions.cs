using NSwag;
using NSwag.Generation.Processors.Security;

namespace InvoiceTrackerApi.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        
        // Add NSwag OpenAPI document generation
        services.AddOpenApiDocument(config =>
        {
            config.DocumentName = "v1";
            config.Title = "Invoice Tracker API";
            config.Version = "v1";
            config.Description = "Microservices Authentication API with Keycloak OAuth2.0";
            
            // Add JWT Bearer authentication
            config.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
            });
            
            config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
            
            // Generate detailed schemas for better TypeScript types
            config.SchemaSettings.GenerateAbstractSchemas = false;
            config.SchemaSettings.GenerateExamples = true;
        });

        return services;
    }
}
