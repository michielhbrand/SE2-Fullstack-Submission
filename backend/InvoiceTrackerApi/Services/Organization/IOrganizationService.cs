using InvoiceTrackerApi.DTOs.Organization.Requests;
using InvoiceTrackerApi.DTOs.Organization.Responses;

namespace InvoiceTrackerApi.Services.Organization;

/// <summary>
/// Service interface for Organization business logic
/// </summary>
public interface IOrganizationService
{
    Task<IEnumerable<OrganizationResponse>> GetAllOrganizationsAsync();
    Task<OrganizationResponse> GetOrganizationByIdAsync(int id);
    Task<OrganizationResponse> CreateOrganizationAsync(CreateOrganizationRequest request);
    Task<OrganizationResponse> UpdateOrganizationAsync(int id, UpdateOrganizationRequest request);
    Task DeleteOrganizationAsync(int id);
    
    // Organization Membership methods
    Task<OrganizationMemberResponse> AddMemberToOrganizationAsync(int organizationId, string userId, AddOrganizationMemberRequest request, string requestingUserId);
    Task RemoveMemberFromOrganizationAsync(int organizationId, string userId, string requestingUserId);
    Task<OrganizationMemberResponse> UpdateMemberRoleAsync(int organizationId, string userId, UpdateMemberRoleRequest request, string requestingUserId);
    Task<IEnumerable<OrganizationMemberResponse>> GetOrganizationMembersAsync(int organizationId);
    Task<IEnumerable<OrganizationResponse>> GetUserOrganizationsAsync(string userId);
}
