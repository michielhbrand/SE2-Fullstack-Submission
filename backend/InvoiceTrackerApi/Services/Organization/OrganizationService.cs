using InvoiceTrackerApi.Data;
using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Organization;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Services.Organization;

/// <summary>
/// Service implementation for Organization business logic
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        ApplicationDbContext context,
        ILogger<OrganizationService> logger)
    {
        _organizationRepository = organizationRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<OrganizationResponse>> GetAllOrganizationsAsync()
    {
        var organizations = await _organizationRepository.GetAllAsync();
        
        var responses = new List<OrganizationResponse>();
        foreach (var org in organizations)
        {
            var bankAccounts = await _context.BankAccounts
                .Where(a => org.BankAccountIds.Contains(a.Id))
                .ToListAsync();
            
            var response = org.ToDto();
            response.BankAccounts = bankAccounts.Select(a => a.ToDto()).ToList();
            responses.Add(response);
        }
        
        return responses;
    }

    public async Task<OrganizationResponse> GetOrganizationByIdAsync(int id)
    {
        var organization = await _organizationRepository.GetByIdWithDetailsAsync(id);

        if (organization == null)
        {
            throw new NotFoundException("Organization", id);
        }

        var bankAccounts = await _context.BankAccounts
            .Where(a => organization.BankAccountIds.Contains(a.Id))
            .ToListAsync();

        var response = organization.ToDto();
        response.BankAccounts = bankAccounts.Select(a => a.ToDto()).ToList();

        return response;
    }

    public async Task<OrganizationResponse> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        // Create address first
        var address = request.Address.ToModel();
        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        // Create organization
        var organization = request.ToModel();
        organization.AddressId = address.Id;

        var createdOrganization = await _organizationRepository.AddAsync(organization);

        _logger.LogInformation("Organization {OrganizationId} created", createdOrganization.Id);

        var response = createdOrganization.ToDto();
        response.BankAccounts = new List<BankAccountResponse>();

        return response;
    }

    public async Task<OrganizationResponse> UpdateOrganizationAsync(int id, UpdateOrganizationRequest request)
    {
        var existingOrganization = await _organizationRepository.GetByIdWithDetailsAsync(id);

        if (existingOrganization == null)
        {
            throw new NotFoundException("Organization", id);
        }

        // Update properties if provided
        if (request.Name != null)
        {
            existingOrganization.Name = request.Name;
        }

        if (request.AddressId.HasValue)
        {
            // Verify address exists
            var addressExists = await _context.Addresses.AnyAsync(a => a.Id == request.AddressId.Value);
            if (!addressExists)
            {
                throw new NotFoundException("Address", request.AddressId.Value);
            }
            existingOrganization.AddressId = request.AddressId.Value;
        }

        if (request.UserIds != null)
        {
            existingOrganization.UserIds = request.UserIds;
        }

        if (request.BankAccountIds != null)
        {
            // Verify all bank accounts exist and belong to this organization
            var bankAccountsToVerify = await _context.BankAccounts
                .Where(a => request.BankAccountIds.Contains(a.Id))
                .ToListAsync();
            
            if (bankAccountsToVerify.Count != request.BankAccountIds.Count)
            {
                throw new NotFoundException("One or more bank accounts not found");
            }

            // Verify all bank accounts belong to this organization
            if (bankAccountsToVerify.Any(a => a.OrganizationId != id))
            {
                throw new BusinessRuleException("All bank accounts must belong to this organization");
            }

            existingOrganization.BankAccountIds = request.BankAccountIds;
        }

        await _organizationRepository.UpdateAsync(existingOrganization);

        _logger.LogInformation("Organization {OrganizationId} updated", id);

        // Reload with details
        var updatedOrganization = await _organizationRepository.GetByIdWithDetailsAsync(id);
        var bankAccounts = await _context.BankAccounts
            .Where(a => updatedOrganization!.BankAccountIds.Contains(a.Id))
            .ToListAsync();

        var response = updatedOrganization!.ToDto();
        response.BankAccounts = bankAccounts.Select(a => a.ToDto()).ToList();

        return response;
    }

    public async Task DeleteOrganizationAsync(int id)
    {
        var organization = await _organizationRepository.GetByIdAsync(id);

        if (organization == null)
        {
            throw new NotFoundException("Organization", id);
        }

        // Delete associated bank accounts
        var bankAccountsToDelete = await _context.BankAccounts
            .Where(a => a.OrganizationId == id)
            .ToListAsync();
        
        _context.BankAccounts.RemoveRange(bankAccountsToDelete);

        // Delete organization
        await _organizationRepository.DeleteAsync(organization);

        _logger.LogInformation("Organization {OrganizationId} deleted", id);
    }
}
