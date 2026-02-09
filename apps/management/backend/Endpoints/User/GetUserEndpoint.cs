using ManagementApi.DTOs.User;
using ManagementApi.Filters;
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
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<GetUserRequest>>();
    }

    private static async Task<Results<Ok<UserResponse>, ProblemHttpResult>> Handle(
        string userId,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var request = new GetUserRequest { UserId = userId };
        var user = await userService.GetUserByIdAsync(request.UserId, cancellationToken);
        return TypedResults.Ok(user);
    }
}
