using ManagementApi.Data;
using ManagementApi.DTOs.Organization;
using ManagementApi.Mappers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagementApi.Endpoints.Organization;

public static class GetAllOrganizationsEndpoint
{
    public static RouteHandlerBuilder MapGetAllOrganizations(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/", Handle)
            .WithName("GetOrganizations")
            .WithSummary("Get all organizations")
            .WithDescription("Retrieves a list of organizations with optional filtering, searching, and sorting")
            .WithOpenApi()
            .Produces<List<OrganizationResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Ok<List<OrganizationResponse>>> Handle(
        [AsParameters] GetOrganizationsRequest query,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("GetAllOrganizations");
        logger.LogInformation("Retrieving organizations with filters - Search: {Search}, Status: {Status}, SortBy: {SortBy}, SortDirection: {SortDirection}",
            query.Search, query.Status, query.SortBy, query.SortDirection);

        var queryable = db.Organizations
            .Include(o => o.Address)
            .Include(o => o.Members)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.ToLower();
            queryable = queryable.Where(o =>
                (o.Name != null && o.Name.ToLower().Contains(searchTerm)) ||
                (o.Email != null && o.Email.ToLower().Contains(searchTerm)) ||
                (o.Phone != null && o.Phone.ToLower().Contains(searchTerm)) ||
                (o.RegistrationNumber != null && o.RegistrationNumber.ToLower().Contains(searchTerm)) ||
                (o.Address != null && o.Address.City != null && o.Address.City.ToLower().Contains(searchTerm)) ||
                (o.Address != null && o.Address.Country != null && o.Address.Country.ToLower().Contains(searchTerm))
            );
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(query.Status) && query.Status.ToLower() != "all")
        {
            var isActive = query.Status.ToLower() == "active";
            queryable = queryable.Where(o => o.Active == isActive);
        }

        // Apply sorting
        queryable = (query.SortBy?.ToLower(), query.SortDirection?.ToLower()) switch
        {
            ("name", "desc") => queryable.OrderByDescending(o => o.Name),
            ("name", _) => queryable.OrderBy(o => o.Name),
            
            ("email", "desc") => queryable.OrderByDescending(o => o.Email),
            ("email", _) => queryable.OrderBy(o => o.Email),
            
            ("city", "desc") => queryable.OrderByDescending(o => o.Address != null ? o.Address.City : null),
            ("city", _) => queryable.OrderBy(o => o.Address != null ? o.Address.City : null),
            
            ("status", "desc") => queryable.OrderByDescending(o => o.Active),
            ("status", _) => queryable.OrderBy(o => o.Active),
            
            ("created", "desc") => queryable.OrderByDescending(o => o.CreatedAt),
            ("created", _) => queryable.OrderBy(o => o.CreatedAt),
            
            _ => queryable.OrderBy(o => o.Name) // Default sort
        };

        var organizations = await queryable.ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} organizations after filtering", organizations.Count);

        var responses = organizations.Select(org => org.ToResponse()).ToList();

        return TypedResults.Ok(responses);
    }
}
