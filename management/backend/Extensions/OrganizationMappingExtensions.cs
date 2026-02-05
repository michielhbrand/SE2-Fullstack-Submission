using ManagementApi.DTOs.Organization;
using ManagementApi.Models;

namespace ManagementApi.Extensions;

/// <summary>
/// Extension methods for mapping domain models to DTOs
/// </summary>
public static class OrganizationMappingExtensions
{
    /// <summary>
    /// Maps an Organization entity to OrganizationResponse DTO
    /// </summary>
    /// <param name="organization">The organization entity</param>
    /// <param name="bankAccounts">Optional list of bank accounts associated with the organization</param>
    /// <returns>OrganizationResponse DTO</returns>
    public static OrganizationResponse ToResponse(this Organization organization, List<BankAccount>? bankAccounts = null)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            TaxNumber = organization.TaxNumber,
            RegistrationNumber = organization.RegistrationNumber,
            Email = organization.Email,
            Phone = organization.Phone,
            Website = organization.Website,
            Address = organization.Address?.ToResponse(),
            BankAccounts = bankAccounts?.Select(ba => ba.ToResponse()).ToList() ?? new List<BankAccountResponse>(),
            MemberCount = organization.Members?.Count ?? 0,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }

    /// <summary>
    /// Maps an Address entity to AddressResponse DTO
    /// </summary>
    /// <param name="address">The address entity</param>
    /// <returns>AddressResponse DTO or null if address is null</returns>
    public static AddressResponse? ToResponse(this Address? address)
    {
        if (address == null) return null;

        return new AddressResponse
        {
            Id = address.Id,
            Street = address.Street,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country
        };
    }

    /// <summary>
    /// Maps a BankAccount entity to BankAccountResponse DTO
    /// </summary>
    /// <param name="bankAccount">The bank account entity</param>
    /// <returns>BankAccountResponse DTO</returns>
    public static BankAccountResponse ToResponse(this BankAccount bankAccount)
    {
        return new BankAccountResponse
        {
            Id = bankAccount.Id,
            AccountName = bankAccount.AccountName,
            AccountNumber = bankAccount.AccountNumber,
            BankName = bankAccount.BankName,
            BranchCode = bankAccount.BranchCode,
            SwiftCode = bankAccount.SwiftCode,
            Active = bankAccount.Active
        };
    }
}
