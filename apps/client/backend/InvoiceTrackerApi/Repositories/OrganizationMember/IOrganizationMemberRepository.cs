using InvoiceTrackerApi.Models;

namespace InvoiceTrackerApi.Repositories.OrganizationMember;

/// <summary>
/// Repository interface for OrganizationMember operations
/// </summary>
public interface IOrganizationMemberRepository
{
    Task<IEnumerable<Models.OrganizationMember>> GetMembersByOrganizationIdAsync(int organizationId);
    Task<IEnumerable<Models.Organization>> GetOrganizationsByUserIdAsync(string userId);
    Task<Models.OrganizationMember?> GetMembershipAsync(int organizationId, string userId);
    Task<Models.OrganizationMember> AddMemberAsync(Models.OrganizationMember member);
    Task UpdateMemberRoleAsync(Models.OrganizationMember member);
    Task RemoveMemberAsync(Models.OrganizationMember member);
    Task<bool> IsMemberAsync(int organizationId, string userId);
    Task<bool> HasRoleAsync(int organizationId, string userId, string role);
}
