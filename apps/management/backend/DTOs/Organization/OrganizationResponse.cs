namespace ManagementApi.DTOs.Organization;

public record OrganizationResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? TaxNumber { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public bool Active { get; init; }
    public AddressResponse? Address { get; init; }
    public List<BankAccountResponse> BankAccounts { get; init; } = new();
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record AddressResponse
{
    public int Id { get; init; }
    public required string Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

public record BankAccountResponse
{
    public int Id { get; init; }
    public required string AccountName { get; init; }
    public required string AccountNumber { get; init; }
    public required string BankName { get; init; }
    public string? BranchCode { get; init; }
    public string? SwiftCode { get; init; }
    public bool Active { get; init; }
}
