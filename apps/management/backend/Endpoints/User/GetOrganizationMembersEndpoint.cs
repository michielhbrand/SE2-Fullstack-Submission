using ManagementApi.DTOs.User;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class GetOrganizationMembersEndpoint
{
    public static RouteHandlerBuilder MapGetOrganizationMembers(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/{organizationId}/members", Handle)
            .WithName("GetOrganizationMembers")
            .WithSummary("Get organization members")
            .WithDescription("Retrieves all members of the specified organization")
            .WithOpenApi()
            .Produces<List<OrganizationMemberResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<List<OrganizationMemberResponse>>, ProblemHttpResult>> Handle(
        int organizationId,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var members = await userService.GetOrganizationMembersAsync(organizationId, cancellationToken);
        return TypedResults.Ok(members);
    }
}
