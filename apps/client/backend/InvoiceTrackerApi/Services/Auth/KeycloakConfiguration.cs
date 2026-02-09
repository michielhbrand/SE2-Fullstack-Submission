namespace InvoiceTrackerApi.Services.Auth;

/// <summary>
/// Holds Keycloak configuration settings extracted from application configuration.
/// </summary>
public class KeycloakConfiguration
{
    public string KeycloakUrl { get; }
    public string Realm { get; }
    public string ClientId { get; }
    public string AdminClientId { get; }
    public string AdminUsername { get; }
    public string AdminPassword { get; }

    public KeycloakConfiguration(IConfiguration configuration, ILogger logger)
    {
        // Extract Keycloak configuration
        var authority = configuration["Keycloak:Authority"] 
            ?? throw new InvalidOperationException("Keycloak:Authority not configured");
        
        // Parse authority to extract base URL and realm
        // Authority format: http://localhost:9090/realms/microservices
        var authorityUri = new Uri(authority);
        var pathSegments = authorityUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        KeycloakUrl = $"{authorityUri.Scheme}://{authorityUri.Authority}";
        Realm = pathSegments.Length >= 2 ? pathSegments[1] : "microservices";
        ClientId = configuration["Keycloak:ClientId"] ?? "frontend-app";
        AdminClientId = "admin-app";
        AdminUsername = configuration["Keycloak:AdminUsername"] ?? "admin";
        AdminPassword = configuration["Keycloak:AdminPassword"] ?? "admin";
        
        logger.LogInformation(
            "KeycloakConfiguration initialized with URL: {Url}, Realm: {Realm}, ClientId: {ClientId}",
            KeycloakUrl, Realm, ClientId);
    }

    public string GetTokenEndpoint() => $"{KeycloakUrl}/realms/{Realm}/protocol/openid-connect/token";
    public string GetLogoutEndpoint() => $"{KeycloakUrl}/realms/{Realm}/protocol/openid-connect/logout";
    public string GetAdminTokenEndpoint() => $"{KeycloakUrl}/realms/master/protocol/openid-connect/token";
    public string GetUsersEndpoint() => $"{KeycloakUrl}/admin/realms/{Realm}/users";
    public string GetUserEndpoint(string userId) => $"{KeycloakUrl}/admin/realms/{Realm}/users/{userId}";
    public string GetRolesEndpoint() => $"{KeycloakUrl}/admin/realms/{Realm}/roles";
    public string GetUserRoleMappingEndpoint(string userId) =>
        $"{KeycloakUrl}/admin/realms/{Realm}/users/{userId}/role-mappings/realm";
}
