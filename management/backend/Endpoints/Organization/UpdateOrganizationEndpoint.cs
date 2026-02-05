using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using ManagementApi.Filters;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Organization;

/// <summary>
/// Endpoint for updating an existing organization
/// </summary>
public static class UpdateOrganizationEndpoint
{
    /// <summary>
    /// Maps the update organization endpoint
    /// </summary>
    public static RouteHandlerBuilder MapUpdateOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapPut("/{id:int}", Handle)
            .WithName("UpdateOrganization")
            .WithOpenApi()
            .AddEndpointFilter<ValidationFilter<UpdateOrganizationRequest>>();
    }

    /// <summary>
    /// Handles updating an existing organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <param name="db">Database context</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
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
