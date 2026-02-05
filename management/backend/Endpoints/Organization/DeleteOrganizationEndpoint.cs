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
            .WithOpenApi();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        int id,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var org = await db.Organizations.FindAsync(new object[] { id }, cancellationToken);
        if (org == null)
            throw new NotFoundException("Organization", id);

        db.Organizations.Remove(org);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
