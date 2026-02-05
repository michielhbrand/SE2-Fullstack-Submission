using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

public static class GetOrganizationByIdEndpoint
{
    public static RouteHandlerBuilder MapGetOrganizationById(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/{id:int}", Handle)
            .WithName("GetOrganizationById")
            .WithOpenApi();
    }

    private static async Task<Results<Ok<OrganizationResponse>, ProblemHttpResult>> Handle(int id, ApplicationDbContext db)
    {
        var org = await db.Organizations
            .Include(o => o.Address)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
            throw new NotFoundException("Organization", id);

        // Get bank accounts
        var bankAccounts = await db.BankAccounts
            .Where(ba => org.BankAccountIds.Contains(ba.Id))
            .ToListAsync();

        var response = new OrganizationResponse
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
            BankAccounts = bankAccounts.Select(ba => new BankAccountResponse
            {
                Id = ba.Id,
                AccountName = ba.AccountName,
                AccountNumber = ba.AccountNumber,
                BankName = ba.BankName,
                BranchCode = ba.BranchCode,
                SwiftCode = ba.SwiftCode,
                Active = ba.Active
            }).ToList(),
            MemberCount = org.Members.Count,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };

        return TypedResults.Ok(response);
    }
}
