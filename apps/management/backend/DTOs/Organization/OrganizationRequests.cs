namespace ManagementApi.DTOs.Organization;

public class GetOrganizationsRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; } = "all";
    public string? SortBy { get; set; } = "name";
    public string? SortDirection { get; set; } = "asc";
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public record CreateOrganizationRequest
{
    public required string Name { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public CreateAddressRequest? Address { get; init; }

    /// <summary>Payment plan ID to assign. Defaults to 1 (Basic) if not provided.</summary>
    public int? PaymentPlanId { get; init; }
}

public record CreateAddressRequest
{
    public required string Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

public record UpdateOrganizationRequest
{
    public string? Name { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public bool? Active { get; init; }
    public CreateAddressRequest? Address { get; init; }

    /// <summary>Payment plan ID to assign to this organization.</summary>
    public int? PaymentPlanId { get; init; }
}
