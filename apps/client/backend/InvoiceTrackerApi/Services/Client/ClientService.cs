using InvoiceTrackerApi.DTOs.Client.Requests;
using InvoiceTrackerApi.DTOs.Client.Responses;
using InvoiceTrackerApi.DTOs.Common;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Client;

namespace InvoiceTrackerApi.Services.Client;

/// <summary>
/// Service implementation for Client business logic
/// </summary>
public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientService> _logger;

    public ClientService(IClientRepository clientRepository, ILogger<ClientService> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ClientResponse>> GetClientsAsync(int page, int pageSize, string? search = null)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var clients = await _clientRepository.GetAllAsync(page, pageSize, search);
        var totalCount = await _clientRepository.GetTotalCountAsync(search);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponse<ClientResponse>
        {
            Data = clients.Select(c => c.ToDto()).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<ClientResponse> GetClientByIdAsync(int id)
    {
        var client = await _clientRepository.GetByIdAsync(id);

        if (client == null)
        {
            throw new NotFoundException("Client", id);
        }

        return client.ToDto();
    }

    public async Task<ClientResponse> CreateClientAsync(CreateClientRequest request, string modifiedBy)
    {
        // Business rule validation: Check for duplicate email
        var existingClient = await _clientRepository.GetByEmailAsync(request.Email);
        if (existingClient != null)
        {
            throw new DuplicateEntityException("Client", "email", request.Email);
        }

        var client = request.ToModel();
        client.DateCreated = DateTime.UtcNow;
        client.ModifiedBy = modifiedBy;
        client.LastModifiedDate = DateTime.UtcNow;

        var createdClient = await _clientRepository.AddAsync(client);

        _logger.LogInformation("Client {ClientId} created by {User}", createdClient.Id, modifiedBy);

        return createdClient.ToDto();
    }

    public async Task<ClientResponse> UpdateClientAsync(int id, UpdateClientRequest request, string modifiedBy)
    {
        var existingClient = await _clientRepository.GetByIdAsync(id);

        if (existingClient == null)
        {
            throw new NotFoundException("Client", id);
        }

        // Business rule validation: Check for duplicate email (excluding current client)
        var clientWithEmail = await _clientRepository.GetByEmailAsync(request.Email);
        if (clientWithEmail != null && clientWithEmail.Id != id)
        {
            throw new DuplicateEntityException("Client", "email", request.Email);
        }

        // Update properties
        existingClient.Name = request.Name;
        existingClient.Surname = request.Surname;
        existingClient.Email = request.Email;
        existingClient.Cellphone = request.Cellphone;
        existingClient.Address = request.Address;
        existingClient.Company = request.Company;
        existingClient.KeycloakUserId = request.KeycloakUserId;
        existingClient.LastModifiedDate = DateTime.UtcNow;
        existingClient.ModifiedBy = modifiedBy;

        await _clientRepository.UpdateAsync(existingClient);

        _logger.LogInformation("Client {ClientId} updated by {User}", id, modifiedBy);

        return existingClient.ToDto();
    }

    public async Task DeleteClientAsync(int id)
    {
        var client = await _clientRepository.GetByIdAsync(id);

        if (client == null)
        {
            throw new NotFoundException("Client", id);
        }

        await _clientRepository.DeleteAsync(client);

        _logger.LogInformation("Client {ClientId} deleted", id);
    }
}
