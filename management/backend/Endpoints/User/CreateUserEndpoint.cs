using ManagementApi.DTOs.User;
using ManagementApi.Filters;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class CreateUserEndpoint
{
    public static RouteHandlerBuilder MapCreateUser(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/", Handle)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .WithDescription("Creates a new user in Keycloak and the local database. Default password is the user's email address.")
            .WithOpenApi()
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<CreateUserRequest>>();
    }

    private static async Task<Results<Created<UserResponse>, ProblemHttpResult>> Handle(
        CreateUserRequest request,
        IUserService userService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("CreateUser");
        logger.LogInformation("Creating new user: {Email}", request.Email);

        var user = await userService.CreateUserAsync(request, cancellationToken);

        logger.LogInformation("Successfully created user with ID: {UserId}", user.Id);

        return TypedResults.Created($"/api/users/{user.Id}", user);
    }
}
