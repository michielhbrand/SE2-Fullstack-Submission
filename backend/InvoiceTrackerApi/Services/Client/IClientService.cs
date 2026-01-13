using InvoiceTrackerApi.DTOs;

namespace InvoiceTrackerApi.Services.Client;

/// <summary>
/// Service interface for Client business logic
/// </summary>
public interface IClientService
{
    Task<PaginatedResponse<ClientDto>> GetClientsAsync(int page, int pageSize, string? search = null);
    Task<ClientDto> GetClientByIdAsync(int id);
    Task<ClientDto> CreateClientAsync(CreateClientRequest request, string modifiedBy);
    Task<ClientDto> UpdateClientAsync(int id, UpdateClientRequest request, string modifiedBy);
    Task DeleteClientAsync(int id);
}
