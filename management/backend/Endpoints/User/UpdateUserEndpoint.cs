using ManagementApi.DTOs.User;
using ManagementApi.Filters;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class UpdateUserEndpoint
{
    public static RouteHandlerBuilder MapUpdateUser(this IEndpointRouteBuilder group)
    {
        return group.MapPut("/{userId}", Handle)
            .WithName("UpdateUser")
            .WithSummary("Update a user")
            .WithDescription("Updates user information in both Keycloak and the local database")
            .WithOpenApi()
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateUserRequest>>();
    }

    private static async Task<Results<Ok<UserResponse>, ProblemHttpResult>> Handle(
        string userId,
        UpdateUserRequest request,
        IUserService userService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("UpdateUser");
        logger.LogInformation("Updating user: {UserId}", userId);

        var user = await userService.UpdateUserAsync(userId, request, cancellationToken);

        logger.LogInformation("Successfully updated user: {UserId}", userId);

        return TypedResults.Ok(user);
    }
}
