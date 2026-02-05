using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using ManagementApi.Filters;
using ManagementApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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

        var org = await db.Organizations
            .Include(o => o.Address)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        
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
        if (request.Active.HasValue)
            org.Active = request.Active.Value;
        
        if (request.Address != null)
        {
            if (org.Address == null)
            {
                // Create new address only if Street is provided (required field)
                if (!string.IsNullOrWhiteSpace(request.Address.Street))
                {
                    org.Address = new Address
                    {
                        Street = request.Address.Street,
                        City = request.Address.City,
                        State = request.Address.State,
                        PostalCode = request.Address.PostalCode,
                        Country = request.Address.Country
                    };
                }
            }
            else
            {
                // Update existing address
                if (!string.IsNullOrWhiteSpace(request.Address.Street))
                    org.Address.Street = request.Address.Street;
                if (request.Address.City != null)
                    org.Address.City = request.Address.City;
                if (request.Address.State != null)
                    org.Address.State = request.Address.State;
                if (request.Address.PostalCode != null)
                    org.Address.PostalCode = request.Address.PostalCode;
                if (request.Address.Country != null)
                    org.Address.Country = request.Address.Country;
            }
        }

        org.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully updated organization with ID: {OrganizationId}", id);

        return TypedResults.NoContent();
    }
}
