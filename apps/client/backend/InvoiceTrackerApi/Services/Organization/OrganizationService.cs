using Shared.Database.Data;
using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;
using InvoiceTrackerApi.Exceptions;
using InvoiceTrackerApi.Mappers;
using InvoiceTrackerApi.Repositories.Organization;
using InvoiceTrackerApi.Repositories.OrganizationMember;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Services.Organization;

/// <summary>
/// Service implementation for Organization business logic
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository memberRepository,
        ApplicationDbContext context,
        ILogger<OrganizationService> logger)
    {
        _organizationRepository = organizationRepository;
        _memberRepository = memberRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<OrganizationResponse>> GetAllOrganizationsAsync()
    {
        var organizations = await _organizationRepository.GetAllAsync();
        
        var responses = new List<OrganizationResponse>();
        foreach (var org in organizations)
        {
            var bankAccounts = org.BankAccountIds != null && org.BankAccountIds.Any()
                ? await _context.BankAccounts
                    .Where(a => org.BankAccountIds.Contains(a.Id))
                    .ToListAsync()
                : new List<Shared.Database.Models.BankAccount>();
            
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

        var bankAccounts = organization.BankAccountIds != null && organization.BankAccountIds.Any()
            ? await _context.BankAccounts
                .Where(a => organization.BankAccountIds.Contains(a.Id))
                .ToListAsync()
            : new List<Shared.Database.Models.BankAccount>();

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

        if (request.VatRate.HasValue)
        {
            existingOrganization.VatRate = request.VatRate.Value;
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
        var bankAccounts = updatedOrganization!.BankAccountIds != null && updatedOrganization.BankAccountIds.Any()
            ? await _context.BankAccounts
                .Where(a => updatedOrganization.BankAccountIds.Contains(a.Id))
                .ToListAsync()
            : new List<Shared.Database.Models.BankAccount>();

        var response = updatedOrganization.ToDto();
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

        // Delete organization (cascade will delete members)
        await _organizationRepository.DeleteAsync(organization);

        _logger.LogInformation("Organization {OrganizationId} deleted", id);
    }

    // Organization Membership Methods

    public async Task<OrganizationMemberResponse> AddMemberToOrganizationAsync(
        int organizationId,
        string userId,
        AddOrganizationMemberRequest request,
        string requestingUserId)
    {
        // Verify organization exists
        var organization = await _organizationRepository.GetByIdAsync(organizationId);
        if (organization == null)
        {
            throw new NotFoundException("Organization", organizationId);
        }

        // Check if requesting user is an orgAdmin
        var isAdmin = await _memberRepository.HasRoleAsync(organizationId, requestingUserId, "orgAdmin");
        if (!isAdmin)
        {
            throw new ForbiddenException("Only organization administrators can add members");
        }

        // Check if user is already a member
        var existingMembership = await _memberRepository.GetMembershipAsync(organizationId, userId);
        if (existingMembership != null)
        {
            throw new ConflictException($"User {userId} is already a member of this organization");
        }

        // Add member
        var member = new Shared.Database.Models.OrganizationMember
        {
            OrganizationId = organizationId,
            UserId = userId,
            Role = request.Role
        };

        await _memberRepository.AddMemberAsync(member);

        _logger.LogInformation(
            "User {UserId} added to organization {OrganizationId} with role {Role}",
            userId, organizationId, request.Role);

        return new OrganizationMemberResponse
        {
            OrganizationId = organizationId,
            UserId = userId,
            Role = request.Role
        };
    }

    public async Task RemoveMemberFromOrganizationAsync(
        int organizationId,
        string userId,
        string requestingUserId)
    {
        // Verify organization exists
        var organization = await _organizationRepository.GetByIdAsync(organizationId);
        if (organization == null)
        {
            throw new NotFoundException("Organization", organizationId);
        }

        // Check if requesting user is an orgAdmin
        var isAdmin = await _memberRepository.HasRoleAsync(organizationId, requestingUserId, "orgAdmin");
        if (!isAdmin)
        {
            throw new ForbiddenException("Only organization administrators can remove members");
        }

        // Get membership
        var membership = await _memberRepository.GetMembershipAsync(organizationId, userId);
        if (membership == null)
        {
            throw new NotFoundException($"User {userId} is not a member of this organization");
        }

        // Remove member
        await _memberRepository.RemoveMemberAsync(membership);

        _logger.LogInformation(
            "User {UserId} removed from organization {OrganizationId}",
            userId, organizationId);
    }

    public async Task<OrganizationMemberResponse> UpdateMemberRoleAsync(
        int organizationId,
        string userId,
        UpdateMemberRoleRequest request,
        string requestingUserId)
    {
        // Verify organization exists
        var organization = await _organizationRepository.GetByIdAsync(organizationId);
        if (organization == null)
        {
            throw new NotFoundException("Organization", organizationId);
        }

        // Check if requesting user is an orgAdmin
        var isAdmin = await _memberRepository.HasRoleAsync(organizationId, requestingUserId, "orgAdmin");
        if (!isAdmin)
        {
            throw new ForbiddenException("Only organization administrators can update member roles");
        }

        // Get membership
        var membership = await _memberRepository.GetMembershipAsync(organizationId, userId);
        if (membership == null)
        {
            throw new NotFoundException($"User {userId} is not a member of this organization");
        }

        // Update role
        membership.Role = request.Role;
        await _memberRepository.UpdateMemberRoleAsync(membership);

        _logger.LogInformation(
            "User {UserId} role updated to {Role} in organization {OrganizationId}",
            userId, request.Role, organizationId);

        return new OrganizationMemberResponse
        {
            OrganizationId = organizationId,
            UserId = userId,
            Role = request.Role
        };
    }

    public async Task<IEnumerable<OrganizationMemberResponse>> GetOrganizationMembersAsync(int organizationId)
    {
        // Verify organization exists
        var organization = await _organizationRepository.GetByIdAsync(organizationId);
        if (organization == null)
        {
            throw new NotFoundException("Organization", organizationId);
        }

        var members = await _memberRepository.GetMembersByOrganizationIdAsync(organizationId);

        return members.Select(m => new OrganizationMemberResponse
        {
            OrganizationId = m.OrganizationId,
            UserId = m.UserId,
            Role = m.Role
        });
    }

    public async Task<IEnumerable<OrganizationResponse>> GetUserOrganizationsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching organizations for user {UserId}", userId);
            
            var organizations = await _memberRepository.GetOrganizationsByUserIdAsync(userId);
            
            _logger.LogInformation("Found {Count} organizations for user {UserId}", organizations.Count(), userId);

            var responses = new List<OrganizationResponse>();
            foreach (var org in organizations)
            {
                try
                {
                    // Ensure Address is loaded
                    if (org.Address == null && org.AddressId > 0)
                    {
                        _logger.LogWarning(
                            "Organization {OrgId} has AddressId {AddressId} but Address is null. Loading explicitly.",
                            org.Id, org.AddressId);
                        
                        org.Address = await _context.Addresses.FindAsync(org.AddressId);
                        
                        if (org.Address == null)
                        {
                            _logger.LogError(
                                "Address {AddressId} not found for organization {OrgId}",
                                org.AddressId, org.Id);
                        }
                    }
                    
                    var bankAccounts = org.BankAccountIds != null && org.BankAccountIds.Any()
                        ? await _context.BankAccounts
                            .Where(a => org.BankAccountIds.Contains(a.Id))
                            .ToListAsync()
                        : new List<Shared.Database.Models.BankAccount>();

                    var response = org.ToDto();
                    response.BankAccounts = bankAccounts.Select(a => a.ToDto()).ToList();
                    responses.Add(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing organization {OrgId} for user {UserId}",
                        org.Id, userId);
                    throw;
                }
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching organizations for user {UserId}", userId);
            throw;
        }
    }
}
