using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.DTOs.Client.Responses;
using Shared.Database.Models;

namespace InvoiceTrackerApi.Mappers;

/// <summary>
/// Extension methods for mapping between Client domain models and DTOs
/// </summary>
public static class ClientMappingExtensions
{
    public static ClientResponse ToDto(this Client client)
    {
        return new ClientResponse
        {
            Id = client.Id,
            Name = client.Name,
            Email = client.Email,
            Cellphone = client.Cellphone,
            Address = client.Address,
            IsCompany = client.IsCompany,
            VatNumber = client.VatNumber,
            OrganizationId = client.OrganizationId,
            DateCreated = client.DateCreated,
            LastModifiedDate = client.LastModifiedDate,
            ModifiedBy = client.ModifiedBy,
            KeycloakUserId = client.KeycloakUserId
        };
    }

    public static Client ToModel(this CreateClientRequest request)
    {
        return new Client
        {
            Name = request.Name,
            Email = request.Email,
            Cellphone = request.Cellphone,
            Address = request.Address,
            IsCompany = request.IsCompany,
            VatNumber = request.VatNumber,
            KeycloakUserId = request.KeycloakUserId
        };
    }
}
