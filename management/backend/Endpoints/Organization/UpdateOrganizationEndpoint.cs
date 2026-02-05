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
            .WithSummary("Update an organization")
            .WithDescription("Updates an existing organization's details by its ID")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateOrganizationRequest>>();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        int id,
        UpdateOrganizationRequest request,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("UpdateOrganization");
        logger.LogInformation("Updating organization with ID: {OrganizationId}", id);

        var org = await db.Organizations.FindAsync(new object[] { id }, cancellationToken);
        if (org == null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found", id);
            throw new NotFoundException("Organization", id);
        }

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

        logger.LogInformation("Successfully updated organization with ID: {OrganizationId}", id);

        return TypedResults.NoContent();
    }
}
