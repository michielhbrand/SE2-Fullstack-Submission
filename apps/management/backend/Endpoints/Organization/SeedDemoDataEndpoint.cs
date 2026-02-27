using ManagementApi.Services.SeedData;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Database.Data;

namespace ManagementApi.Endpoints.Organization;

public static class SeedDemoDataEndpoint
{
    public static RouteHandlerBuilder MapSeedDemoData(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/{id:int}/seed-demo-data", Handle)
            .WithName("SeedDemoData")
            .WithSummary("Seed demo data for an organization")
            .WithDescription("Generates a realistic pre-populated dataset for the specified organization")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok, ProblemHttpResult>> Handle(
        int id,
        ISeedDemoDataService seedService,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("SeedDemoData");
        logger.LogInformation("Seeding demo data requested for organization {OrganizationId}", id);

        var exists = await db.Organizations.AnyAsync(o => o.Id == id, cancellationToken);
        if (!exists)
        {
            logger.LogWarning("Organization {OrganizationId} not found", id);
            return TypedResults.Problem(
                detail: $"Organization with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        await seedService.SeedDemoDataAsync(id, cancellationToken);
        return TypedResults.Ok();
    }
}
