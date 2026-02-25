using ManagementApi.DTOs.Organization;
using ManagementApi.DTOs.PaymentPlan;
using Shared.Database.Models;

namespace ManagementApi.Mappers;

/// <summary>
/// Mapper for converting Organization domain models to DTOs
/// </summary>
public static class OrganizationMapper
{
    /// <summary>
    /// Maps an Organization entity to OrganizationResponse DTO
    /// </summary>
    /// <param name="organization">The organization entity</param>
    /// <returns>OrganizationResponse DTO</returns>
    public static OrganizationResponse ToResponse(this Organization organization)
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
            Active = organization.Active,
            Address = organization.Address?.ToResponse(),
            PaymentPlan = organization.PaymentPlan == null ? null : new PaymentPlanResponse
            {
                Id = organization.PaymentPlan.Id,
                Name = organization.PaymentPlan.Name,
                MaxUsers = organization.PaymentPlan.MaxUsers,
                MonthlyCostRand = organization.PaymentPlan.MonthlyCostRand
            },
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
}
