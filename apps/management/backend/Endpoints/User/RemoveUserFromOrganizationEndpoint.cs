using ManagementApi.DTOs.User;
using ManagementApi.Filters;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class RemoveUserFromOrganizationEndpoint
{
    public static RouteHandlerBuilder MapRemoveUserFromOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapDelete("/{organizationId}/members/{userId}", Handle)
            .WithName("RemoveUserFromOrganization")
            .WithSummary("Remove a user from an organization")
            .WithDescription("Removes a user's membership from the specified organization")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<RemoveUserFromOrganizationRequest>>();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        int organizationId,
        string userId,
        IUserService userService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var request = new RemoveUserFromOrganizationRequest
        {
            OrganizationId = organizationId,
            UserId = userId
        };
        
        var logger = loggerFactory.CreateLogger("RemoveUserFromOrganization");
        logger.LogInformation("Removing user {UserId} from organization {OrganizationId}", request.UserId, request.OrganizationId);

        await userService.RemoveUserFromOrganizationAsync(request.OrganizationId, request.UserId, cancellationToken);

        logger.LogInformation("Successfully removed user {UserId} from organization {OrganizationId}", request.UserId, request.OrganizationId);

        return TypedResults.NoContent();
    }
}
