using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.DTOs.Client.Responses;
using InvoiceTrackerApi.DTOs.Common;

namespace InvoiceTrackerApi.Services.Client;

/// <summary>
/// Service interface for Client business logic
/// </summary>
public interface IClientService
{
    Task<PaginatedResponse<ClientResponse>> GetClientsAsync(int page, int pageSize, string? search = null);
    Task<ClientResponse> GetClientByIdAsync(int id);
    Task<ClientResponse> CreateClientAsync(CreateClientRequest request, string modifiedBy);
    Task<ClientResponse> UpdateClientAsync(int id, UpdateClientRequest request, string modifiedBy);
    Task DeleteClientAsync(int id);
}
