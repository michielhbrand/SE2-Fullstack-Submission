using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ManagementApi.Endpoints.User;

/// <summary>
/// Endpoint for querying the UserDirectory read model with pagination, filtering, and sorting
/// </summary>
public static class GetUserDirectoryEndpoint
{
    public static RouteHandlerBuilder MapGetUserDirectory(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/directory", Handle)
            .WithName("GetUserDirectory")
            .WithSummary("Get users from UserDirectory with pagination and filtering")
            .WithDescription("Queries the UserDirectory read model for fast, denormalized user data suitable for UI tables")
            .WithOpenApi()
            .Produces<PagedUserDirectoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<PagedUserDirectoryResponse>, ProblemHttpResult>> Handle(
        [FromServices] IUserDirectoryService userDirectoryService,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = "Email",
        [FromQuery] bool sortDescending = false,
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new UserDirectoryQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDescending = sortDescending,
            ActiveOnly = activeOnly
        };

        var result = await userDirectoryService.GetUsersAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
