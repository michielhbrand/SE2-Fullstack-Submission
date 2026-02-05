using ManagementApi.DTOs.User;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class GetAllUsersEndpoint
{
    public static RouteHandlerBuilder MapGetAllUsers(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/", Handle)
            .WithName("GetAllUsers")
            .WithSummary("Get all users")
            .WithDescription("Retrieves all users in the system")
            .WithOpenApi()
            .Produces<List<UserResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<List<UserResponse>>, ProblemHttpResult>> Handle(
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var users = await userService.GetAllUsersAsync(cancellationToken);
        return TypedResults.Ok(users);
    }
}
