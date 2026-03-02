using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace Shared.Core.Extensions;

public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddKeycloakJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool mapInboundClaims = false,
        string nameClaimType = "preferred_username")
    {
        var audience = configuration["Keycloak:Audience"] ?? "backend-api";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.Audience = audience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = mapInboundClaims;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudiences = [audience],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = nameClaimType,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogError(context.Exception, "Authentication failed");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            var realmAccessClaim = identity.FindFirst("realm_access");
                            if (realmAccessClaim != null)
                            {
                                try
                                {
                                    var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
                                    if (realmAccess.TryGetProperty("roles", out var rolesElement))
                                    {
                                        var roles = rolesElement.EnumerateArray()
                                            .Select(r => r.GetString())
                                            .Where(r => !string.IsNullOrEmpty(r))
                                            .ToList();

                                        foreach (var role in roles)
                                        {
                                            if (!string.IsNullOrEmpty(role))
                                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                        }

                                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                                        logger.LogInformation("Token validated with roles: {Roles}", string.Join(", ", roles));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                                    logger.LogError(ex, "Error extracting roles from realm_access claim");
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("Authentication challenge: {Error}, {ErrorDescription}",
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
