using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

/// <summary>
/// Endpoint for retrieving all organizations
/// </summary>
public static class GetAllOrganizationsEndpoint
{
    /// <summary>
    /// Maps the get all organizations endpoint
    /// </summary>
    public static RouteHandlerBuilder MapGetAllOrganizations(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/", Handle)
            .WithName("GetOrganizations")
            .WithOpenApi();
    }

    /// <summary>
    /// Handles retrieving all organizations
    /// </summary>
    /// <param name="db">Database context</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of organization responses</returns>
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
