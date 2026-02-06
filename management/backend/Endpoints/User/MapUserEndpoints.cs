using Microsoft.AspNetCore.Authorization;

namespace ManagementApi.Endpoints.User;

public static class UserEndpointsMapper
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var usersGroup = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "systemAdmin" });

        // Write operations (create, update)
        usersGroup.MapCreateUser();
        usersGroup.MapUpdateUser();
        
        // Read operations (using UserDirectory read model)
        usersGroup.MapGetUserDirectory();
        usersGroup.MapGetAllUsers();
        usersGroup.MapGetUser();
        
        // Sync operations
        usersGroup.MapSyncUserDirectory();

        var organizationsGroup = app.MapGroup("/api/organizations")
            .WithTags("Organization Members")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "systemAdmin" });

        organizationsGroup.MapAddUserToOrganization();
        organizationsGroup.MapGetOrganizationMembers();
        organizationsGroup.MapRemoveUserFromOrganization();
    }
}
