using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.DTOs.Client.Responses;
using InvoiceTrackerApi.Models;

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
            Surname = client.Surname,
            Email = client.Email,
            Cellphone = client.Cellphone,
            Address = client.Address,
            Company = client.Company,
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
            Surname = request.Surname,
            Email = request.Email,
            Cellphone = request.Cellphone,
            Address = request.Address,
            Company = request.Company,
            KeycloakUserId = request.KeycloakUserId
        };
    }
}
