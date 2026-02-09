using ManagementApi.DTOs.User;
using ManagementApi.Filters;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

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
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<GetUserDirectoryRequest>>();
    }

    private static async Task<Results<Ok<PagedUserDirectoryResponse>, ProblemHttpResult>> Handle(
        [AsParameters] GetUserDirectoryRequest request,
        IUserDirectoryService userDirectoryService,
        CancellationToken cancellationToken)
    {
        var query = new UserDirectoryQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending,
            ActiveOnly = request.ActiveOnly
        };

        var result = await userDirectoryService.GetUsersAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
