using Shared.Database.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using ManagementApi.Mappers;
using ManagementApi.Filters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

public static class CreateOrganizationEndpoint
{
    public static RouteHandlerBuilder MapCreateOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/", Handle)
            .WithName("CreateOrganization")
            .WithSummary("Create a new organization")
            .WithDescription("Creates a new organization with the provided details including optional address information")
            .WithOpenApi()
            .Produces<OrganizationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<CreateOrganizationRequest>>();
    }

    private static async Task<Results<Created<OrganizationResponse>, ProblemHttpResult>> Handle(
        CreateOrganizationRequest request,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("CreateOrganization");
        logger.LogInformation("Creating new organization: {OrganizationName}", request.Name);

        // Resolve payment plan (default to Basic / Id=1)
        var planId = request.PaymentPlanId ?? 1;
        var planExists = await db.PaymentPlans.AnyAsync(p => p.Id == planId, cancellationToken);
        if (!planExists)
            throw new NotFoundException("PaymentPlan", planId);

        var organization = new Shared.Database.Models.Organization
        {
            Name = request.Name,
            TaxNumber = request.TaxNumber,
            RegistrationNumber = request.RegistrationNumber,
            Email = request.Email,
            Phone = request.Phone,
            Website = request.Website,
            PaymentPlanId = planId,
            CreatedAt = DateTime.UtcNow
        };

        // Create address if provided
        if (request.Address != null)
        {
            logger.LogDebug("Creating address for organization: {OrganizationName}", request.Name);
            organization.Address = new Shared.Database.Models.Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                Country = request.Address.Country
            };
        }

        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully created organization with ID: {OrganizationId}", organization.Id);

        // Reload with PaymentPlan navigation property for the response
        await db.Entry(organization).Reference(o => o.PaymentPlan).LoadAsync(cancellationToken);

        var response = organization.ToResponse();

        return TypedResults.Created($"/api/organizations/{organization.Id}", response);
    }
}
