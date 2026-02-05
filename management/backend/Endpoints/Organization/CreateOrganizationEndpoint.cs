using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using ManagementApi.Filters;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ManagementApi.Endpoints.Organization;

public static class CreateOrganizationEndpoint
{
    public static RouteHandlerBuilder MapCreateOrganization(this IEndpointRouteBuilder group)
    {
        return group.MapPost("/", Handle)
            .WithName("CreateOrganization")
            .WithOpenApi()
            .AddEndpointFilter<ValidationFilter<CreateOrganizationRequest>>();
    }

    private static async Task<Results<Created<OrganizationResponse>, ProblemHttpResult>> Handle(
        CreateOrganizationRequest request,
        ApplicationDbContext db)
    {
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
            var address = new Models.Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                Country = request.Address.Country
            };
            db.Addresses.Add(address);
            await db.SaveChangesAsync();
            organization.AddressId = address.Id;
        }

        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

        var response = new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            TaxNumber = organization.TaxNumber,
            RegistrationNumber = organization.RegistrationNumber,
            Email = organization.Email,
            Phone = organization.Phone,
            Website = organization.Website,
            Address = organization.Address != null ? new AddressResponse
            {
                Id = organization.Address.Id,
                Street = organization.Address.Street,
                City = organization.Address.City,
                State = organization.Address.State,
                PostalCode = organization.Address.PostalCode,
                Country = organization.Address.Country
            } : null,
            BankAccounts = new List<BankAccountResponse>(),
            MemberCount = 0,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };

        return TypedResults.Created($"/api/organizations/{organization.Id}", response);
    }
}
