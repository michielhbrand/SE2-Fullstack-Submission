using ManagementApi.Data;
using ManagementApi.Exceptions.Application;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Organization;

public static class DeleteOrganizationEndpoint
{
    public static RouteHandlerBuilder MapDeleteOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteOrganization")
            .WithSummary("Delete an organization")
            .WithDescription("Deletes an organization by its ID")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        int id,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("DeleteOrganization");
        logger.LogInformation("Deleting organization with ID: {OrganizationId}", id);

        var org = await db.Organizations.FindAsync(new object[] { id }, cancellationToken);
        if (org == null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found", id);
            throw new NotFoundException("Organization", id);
        }

        db.Organizations.Remove(org);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully deleted organization with ID: {OrganizationId}", id);

        return TypedResults.NoContent();
    }
}
