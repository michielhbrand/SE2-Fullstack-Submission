using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Exceptions.Application;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .RequireAuthorization("SystemAdminOnly")
            .WithTags("Organizations");

        // GET /api/organizations - Get all organizations
        group.MapGet("/", GetAllOrganizations)
            .WithName("GetOrganizations")
            .WithOpenApi();

        // GET /api/organizations/{id} - Get organization by ID
        group.MapGet("/{id:int}", GetOrganizationById)
            .WithName("GetOrganizationById")
            .WithOpenApi();

        // POST /api/organizations - Create new organization
        group.MapPost("/", CreateOrganization)
            .WithName("CreateOrganization")
            .WithOpenApi();

        // PUT /api/organizations/{id} - Update organization
        group.MapPut("/{id:int}", UpdateOrganization)
            .WithName("UpdateOrganization")
            .WithOpenApi();

        // DELETE /api/organizations/{id} - Delete organization
        group.MapDelete("/{id:int}", DeleteOrganization)
            .WithName("DeleteOrganization")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetAllOrganizations(ApplicationDbContext db)
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

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetOrganizationById(int id, ApplicationDbContext db)
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

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateOrganization(
        CreateOrganizationRequest request,
        ApplicationDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Organization name is required");

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

        return Results.Created($"/api/organizations/{organization.Id}", response);
    }

    private static async Task<IResult> UpdateOrganization(
        int id,
        UpdateOrganizationRequest request,
        ApplicationDbContext db)
    {
        var org = await db.Organizations.FindAsync(id);
        if (org == null)
            throw new NotFoundException("Organization", id);

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

        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteOrganization(int id, ApplicationDbContext db)
    {
        var org = await db.Organizations.FindAsync(id);
        if (org == null)
            throw new NotFoundException("Organization", id);

        db.Organizations.Remove(org);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
