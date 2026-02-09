using Microsoft.AspNetCore.Authorization;

namespace ManagementApi.Endpoints.User;

public static class UserEndpointsConfiguration
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var usersGroup = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "systemAdmin" });

        usersGroup.MapCreateUser();
        usersGroup.MapUpdateUser();
        usersGroup.MapGetUserDirectory();
        usersGroup.MapGetAllUsers();
        usersGroup.MapGetUser();
        usersGroup.MapSyncUserDirectory();

        var organizationsGroup = app.MapGroup("/api/organizations")
            .WithTags("Organization Members")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "systemAdmin" });

        organizationsGroup.MapAddUserToOrganization();
        organizationsGroup.MapGetOrganizationMembers();
        organizationsGroup.MapRemoveUserFromOrganization();
    }
}
