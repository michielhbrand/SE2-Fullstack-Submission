using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between Organization domain models and DTOs
/// </summary>
public static class OrganizationMappingExtensions
{
    public static OrganizationResponse ToDto(this Organization organization)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Address = organization.Address?.ToDto() ?? new AddressResponse
            {
                Id = 0,
                Street = "",
                City = "",
            }
        };
    }

    public static Organization ToModel(this CreateOrganizationRequest request)
    {
        return new Organization
        {
            Name = request.Name
        };
    }

    // Address mappings
    public static AddressResponse ToDto(this Address address)
    {
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

    public static Address ToModel(this CreateAddressRequest request)
    {
        return new Address
        {
            Street = request.Street,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country
        };
    }

    // BankAccount mappings
    public static BankAccountResponse ToDto(this BankAccount bankAccount)
    {
        return new BankAccountResponse
        {
            Id = bankAccount.Id,
            BankName = bankAccount.BankName,
            BranchCode = bankAccount.BranchCode,
            AccountNumber = bankAccount.AccountNumber,
            AccountType = bankAccount.AccountType,
            Active = bankAccount.Active
        };
    }

    public static BankAccount ToModel(this CreateBankAccountRequest request, int organizationId)
    {
        return new BankAccount
        {
            BankName = request.BankName,
            BranchCode = request.BranchCode,
            AccountNumber = request.AccountNumber,
            AccountType = request.AccountType,
            Active = request.Active,
            OrganizationId = organizationId
        };
    }
}
