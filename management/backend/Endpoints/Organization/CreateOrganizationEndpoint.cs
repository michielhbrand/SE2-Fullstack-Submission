using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Extensions;
using ManagementApi.Filters;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Organization;

/// <summary>
/// Endpoint for creating a new organization
/// </summary>
public static class CreateOrganizationEndpoint
{
    /// <summary>
    /// Maps the create organization endpoint
    /// </summary>
    public static RouteHandlerBuilder MapCreateOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/", Handle)
            .WithName("CreateOrganization")
            .WithOpenApi()
            .AddEndpointFilter<ValidationFilter<CreateOrganizationRequest>>();
    }

    /// <summary>
    /// Handles the creation of a new organization
    /// </summary>
    /// <param name="request">The organization creation request</param>
    /// <param name="db">Database context</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created organization response</returns>
    private static async Task<Results<Created<OrganizationResponse>, ProblemHttpResult>> Handle(
        CreateOrganizationRequest request,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("CreateOrganization");
        logger.LogInformation("Creating new organization: {OrganizationName}", request.Name);

        var organization = new Models.Organization
        {
            Name = request.Name,
            TaxNumber = request.TaxNumber,
            RegistrationNumber = request.RegistrationNumber,
            Email = request.Email,
            Phone = request.Phone,
            Website = request.Website,
            CreatedAt = DateTime.UtcNow
        };

        // Create address if provided
        if (request.Address != null)
        {
            logger.LogDebug("Creating address for organization: {OrganizationName}", request.Name);
            organization.Address = new Models.Address
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

        var response = organization.ToResponse();

        return TypedResults.Created($"/api/organizations/{organization.Id}", response);
    }
}
