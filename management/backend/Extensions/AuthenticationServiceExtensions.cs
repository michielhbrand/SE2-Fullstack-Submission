using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace ManagementApi.Extensions;

public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.Audience = configuration["Keycloak:Audience"];
                options.RequireHttpsMetadata = false; // For development only
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType = ClaimTypes.Role
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "Authentication failed");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Extract roles from Keycloak's realm_access claim
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
                                            {
                                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                            }
                                        }

                                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                        logger.LogInformation("Token validated with roles: {Roles}", string.Join(", ", roles));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                    logger.LogError(ex, "Error extracting roles from realm_access claim");
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authentication challenge: {Error}, {ErrorDescription}",
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("SystemAdminOnly", policy =>
                policy.RequireRole("systemAdmin"));
        });

        return services;
    }
}
