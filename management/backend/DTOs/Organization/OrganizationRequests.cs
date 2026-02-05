namespace ManagementApi.DTOs.Organization;

public record CreateOrganizationRequest
{
    public required string Name { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public CreateAddressRequest? Address { get; init; }
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
}
