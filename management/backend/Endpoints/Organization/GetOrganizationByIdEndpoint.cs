using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using ManagementApi.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

/// <summary>
/// Endpoint for retrieving a specific organization by ID
/// </summary>
public static class GetOrganizationByIdEndpoint
{
    /// <summary>
    /// Maps the get organization by ID endpoint
    /// </summary>
    public static RouteHandlerBuilder MapGetOrganizationById(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/{id:int}", Handle)
            .WithName("GetOrganizationById")
            .WithOpenApi();
    }

    /// <summary>
    /// Handles retrieving a specific organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="db">Database context</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Organization response</returns>
    private static async Task<Results<Ok<OrganizationResponse>, ProblemHttpResult>> Handle(
        int id,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("GetOrganizationById");
        logger.LogInformation("Retrieving organization with ID: {OrganizationId}", id);

        var org = await db.Organizations
            .Include(o => o.Address)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (org == null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found", id);
            throw new NotFoundException("Organization", id);
        }

        // Get bank accounts
        var bankAccounts = await db.BankAccounts
            .Where(ba => org.BankAccountIds.Contains(ba.Id))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved organization with ID: {OrganizationId}", id);

        var response = org.ToResponse(bankAccounts);

        return TypedResults.Ok(response);
    }
}
