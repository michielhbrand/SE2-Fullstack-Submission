using ManagementApi.DTOs.User;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class GetUserEndpoint
{
    public static RouteHandlerBuilder MapGetUser(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/{userId}", Handle)
            .WithName("GetUser")
            .WithSummary("Get user by ID")
            .WithDescription("Retrieves a user by their ID")
            .WithOpenApi()
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<UserResponse>, ProblemHttpResult>> Handle(
        string userId,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetUserByIdAsync(userId, cancellationToken);
        return TypedResults.Ok(user);
    }
}
