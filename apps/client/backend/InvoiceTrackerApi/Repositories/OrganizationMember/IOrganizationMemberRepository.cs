using InvoiceTrackerApi.Models;

namespace InvoiceTrackerApi.Repositories.OrganizationMember;

/// <summary>
/// Repository interface for OrganizationMember operations
/// </summary>
public interface IOrganizationMemberRepository
{
    /// <summary>
    /// Get all members of an organization
    /// </summary>
    Task<IEnumerable<Models.OrganizationMember>> GetMembersByOrganizationIdAsync(int organizationId);
    
    /// <summary>
    /// Get all organizations for a specific user (by Keycloak user ID)
    /// </summary>
    Task<IEnumerable<Models.Organization>> GetOrganizationsByUserIdAsync(string userId);
    
    /// <summary>
    /// Get a specific membership record
    /// </summary>
    Task<Models.OrganizationMember?> GetMembershipAsync(int organizationId, string userId);
    
    /// <summary>
    /// Add a user to an organization with a specific role
    /// </summary>
    Task<Models.OrganizationMember> AddMemberAsync(Models.OrganizationMember member);
    
    /// <summary>
    /// Update a member's role
    /// </summary>
    Task UpdateMemberRoleAsync(Models.OrganizationMember member);
    
    /// <summary>
    /// Remove a user from an organization
    /// </summary>
    Task RemoveMemberAsync(Models.OrganizationMember member);
    
    /// <summary>
    /// Check if a user is a member of an organization
    /// </summary>
    Task<bool> IsMemberAsync(int organizationId, string userId);
    
    /// <summary>
    /// Check if a user has a specific role in an organization
    /// </summary>
    Task<bool> HasRoleAsync(int organizationId, string userId, string role);
}
