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

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, ApplicationDbContext db)
    {
        var org = await db.Organizations.FindAsync(id);
        if (org == null)
            throw new NotFoundException("Organization", id);

        db.Organizations.Remove(org);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
