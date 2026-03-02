using Shared.Database.Data;
using ManagementApi.DTOs.Organization;
using Shared.Core.Exceptions.Application;
using ManagementApi.Mappers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

public static class GetOrganizationByIdEndpoint
{
    public static RouteHandlerBuilder MapGetOrganizationById(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/{id:int}", Handle)
            .WithName("GetOrganizationById")
            .WithSummary("Get organization by ID")
            .WithDescription("Retrieves a specific organization by its ID including address and members")
            .WithOpenApi()
            .Produces<OrganizationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

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
            .Include(o => o.PaymentPlan)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (org == null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found", id);
            throw new NotFoundException("Organization", id);
        }

        logger.LogInformation("Successfully retrieved organization with ID: {OrganizationId}", id);

        var response = org.ToResponse();

        return TypedResults.Ok(response);
    }
}
