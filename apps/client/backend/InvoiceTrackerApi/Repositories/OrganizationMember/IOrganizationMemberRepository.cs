using Shared.Database.Models;

namespace InvoiceTrackerApi.Repositories.OrganizationMember;

/// <summary>
/// Repository interface for OrganizationMember operations
/// </summary>
public interface IOrganizationMemberRepository
{
    Task<IEnumerable<Shared.Database.Models.OrganizationMember>> GetMembersByOrganizationIdAsync(int organizationId);
    Task<IEnumerable<Shared.Database.Models.Organization>> GetOrganizationsByUserIdAsync(string userId);
    Task<Shared.Database.Models.OrganizationMember?> GetMembershipAsync(int organizationId, string userId);
    Task<Shared.Database.Models.OrganizationMember> AddMemberAsync(Shared.Database.Models.OrganizationMember member);
    Task UpdateMemberRoleAsync(Shared.Database.Models.OrganizationMember member);
    Task RemoveMemberAsync(Shared.Database.Models.OrganizationMember member);
    Task<bool> IsMemberAsync(int organizationId, string userId);
    Task<bool> HasRoleAsync(int organizationId, string userId, string role);
    Task<bool> BelongsToAnyOrganizationAsync(string userId);
}
