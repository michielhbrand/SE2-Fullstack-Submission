using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

/// <summary>
/// Endpoint for syncing users from Keycloak to UserDirectory
/// </summary>
public static class SyncUserDirectoryEndpoint
{
    public static RouteHandlerBuilder MapSyncUserDirectory(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/directory/sync", Handle)
            .WithName("SyncUserDirectory")
            .WithSummary("Sync all users from Keycloak to UserDirectory")
            .WithDescription("Triggers a full sync of all users from Keycloak to the UserDirectory read model")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok, ProblemHttpResult>> Handle(
        IUserDirectoryService userDirectoryService,
        CancellationToken cancellationToken)
    {
        await userDirectoryService.SyncAllUsersAsync(cancellationToken);
        return TypedResults.Ok();
    }
}
