using ManagementApi.DTOs.User;
using ManagementApi.Filters;
using ManagementApi.Services.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.User;

public static class AddUserToOrganizationEndpoint
{
    public static RouteHandlerBuilder MapAddUserToOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/{organizationId}/members", Handle)
            .WithName("AddUserToOrganization")
            .WithSummary("Add a user to an organization")
            .WithDescription("Creates a new user (if needed) and adds them to the specified organization with the given role")
            .WithOpenApi()
            .Produces<OrganizationMemberResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<CreateOrganizationMemberRequest>>();
    }

    private static async Task<Results<Created<OrganizationMemberResponse>, ProblemHttpResult>> Handle(
        int organizationId,
        CreateOrganizationMemberRequest request,
        IUserService userService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AddUserToOrganization");
        logger.LogInformation("Adding user {Email} to organization {OrganizationId}", request.Email, organizationId);

        var member = await userService.AddUserToOrganizationAsync(organizationId, request, cancellationToken);

        logger.LogInformation("Successfully added user {UserId} to organization {OrganizationId}", member.UserId, organizationId);

        return TypedResults.Created($"/api/organizations/{organizationId}/members/{member.UserId}", member);
    }
}
