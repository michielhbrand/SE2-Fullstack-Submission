using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using InvoiceTrackerApi.Models;

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
                FirstLine = "",
                City = "",
                Code = ""
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
            FirstLine = address.FirstLine,
            SecondLine = address.SecondLine,
            City = address.City,
            Code = address.Code
        };
    }

    public static Address ToModel(this CreateAddressRequest request)
    {
        return new Address
        {
            FirstLine = request.FirstLine,
            SecondLine = request.SecondLine,
            City = request.City,
            Code = request.Code
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
