using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

public static class GetAllOrganizationsEndpoint
{
    public static RouteHandlerBuilder MapGetAllOrganizations(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/", Handle)
            .WithName("GetOrganizations")
            .WithSummary("Get all organizations")
            .WithDescription("Retrieves a list of all organizations including their addresses and members")
            .WithOpenApi()
            .Produces<List<OrganizationResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Ok<List<OrganizationResponse>>> Handle(
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("GetAllOrganizations");
        logger.LogInformation("Retrieving all organizations");

        var organizations = await db.Organizations
            .Include(o => o.Address)
            .Include(o => o.Members)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} organizations", organizations.Count);

        var responses = organizations.Select(org => org.ToResponse()).ToList();

        return TypedResults.Ok(responses);
    }
}
