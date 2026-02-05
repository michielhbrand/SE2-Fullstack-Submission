using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using ManagementApi.Filters;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Organization;

public static class UpdateOrganizationEndpoint
{
    public static RouteHandlerBuilder MapUpdateOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapPut("/{id:int}", Handle)
            .WithName("UpdateOrganization")
            .WithOpenApi()
            .AddEndpointFilter<ValidationFilter<UpdateOrganizationRequest>>();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        int id,
        UpdateOrganizationRequest request,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var org = await db.Organizations.FindAsync(new object[] { id }, cancellationToken);
        if (org == null)
            throw new NotFoundException("Organization", id);

        if (!string.IsNullOrWhiteSpace(request.Name))
            org.Name = request.Name;
        if (request.TaxNumber != null)
            org.TaxNumber = request.TaxNumber;
        if (request.RegistrationNumber != null)
            org.RegistrationNumber = request.RegistrationNumber;
        if (request.Email != null)
            org.Email = request.Email;
        if (request.Phone != null)
            org.Phone = request.Phone;
        if (request.Website != null)
            org.Website = request.Website;

        org.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
