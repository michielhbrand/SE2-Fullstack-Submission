using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

public static class GetAllOrganizationsEndpoint
{
    public static RouteHandlerBuilder MapGetAllOrganizations(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/", Handle)
            .WithName("GetOrganizations")
            .WithOpenApi();
    }

    private static async Task<Ok<List<OrganizationResponse>>> Handle(ApplicationDbContext db)
    {
        var organizations = await db.Organizations
            .Include(o => o.Address)
            .Include(o => o.Members)
            .ToListAsync();

        var responses = organizations.Select(org => new OrganizationResponse
        {
            Id = org.Id,
            Name = org.Name,
            TaxNumber = org.TaxNumber,
            RegistrationNumber = org.RegistrationNumber,
            Email = org.Email,
            Phone = org.Phone,
            Website = org.Website,
            Address = org.Address != null ? new AddressResponse
            {
                Id = org.Address.Id,
                Street = org.Address.Street,
                City = org.Address.City,
                State = org.Address.State,
                PostalCode = org.Address.PostalCode,
                Country = org.Address.Country
            } : null,
            BankAccounts = new List<BankAccountResponse>(),
            MemberCount = org.Members.Count,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        }).ToList();

        return TypedResults.Ok(responses);
    }
}
