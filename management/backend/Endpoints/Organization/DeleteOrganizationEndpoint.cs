using ManagementApi.Data;
using ManagementApi.Exceptions.Application;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Organization;

/// <summary>
/// Endpoint for deleting an organization
/// </summary>
public static class DeleteOrganizationEndpoint
{
    /// <summary>
    /// Maps the delete organization endpoint
    /// </summary>
    public static RouteHandlerBuilder MapDeleteOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteOrganization")
            .WithOpenApi();
    }

    /// <summary>
    /// Handles deleting an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="db">Database context</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
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
