using Shared.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Repositories.OrganizationMember;

/// <summary>
/// Repository implementation for OrganizationMember operations
/// </summary>
public class OrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly ApplicationDbContext _context;

    public OrganizationMemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Shared.Database.Models.OrganizationMember>> GetMembersByOrganizationIdAsync(int organizationId)
    {
        return await _context.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shared.Database.Models.Organization>> GetOrganizationsByUserIdAsync(string userId)
    {
        return await _context.Organizations
            .Include(o => o.Address)
            .Include(o => o.Members)
            .Where(o => o.Members.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<Shared.Database.Models.OrganizationMember?> GetMembershipAsync(int organizationId, string userId)
    {
        return await _context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
    }

    public async Task<Shared.Database.Models.OrganizationMember> AddMemberAsync(Shared.Database.Models.OrganizationMember member)
    {
        _context.OrganizationMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task UpdateMemberRoleAsync(Shared.Database.Models.OrganizationMember member)
    {
        _context.OrganizationMembers.Update(member);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Shared.Database.Models.OrganizationMember member)
    {
        _context.OrganizationMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsMemberAsync(int organizationId, string userId)
    {
        return await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
    }

    public async Task<bool> HasRoleAsync(int organizationId, string userId, string role)
    {
        return await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organizationId
                        && m.UserId == userId
                        && m.Role == role);
    }

    public async Task<bool> BelongsToAnyOrganizationAsync(string userId)
    {
        return await _context.OrganizationMembers
            .AnyAsync(m => m.UserId == userId);
    }
}
