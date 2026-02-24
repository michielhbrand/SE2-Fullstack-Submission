using ManagementApi.DTOs.User;
using Shared.Database.Models;

namespace ManagementApi.Services.User;

public interface IUserService
{
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserResponse> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserWithOrganizationsResponse> GetUserWithOrganizationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<List<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    // Organization member management
    Task<OrganizationMemberResponse> AddUserToOrganizationAsync(int organizationId, CreateOrganizationMemberRequest request, CancellationToken cancellationToken = default);
    Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync(int organizationId, CancellationToken cancellationToken = default);
    Task RemoveUserFromOrganizationAsync(int organizationId, string userId, CancellationToken cancellationToken = default);
}
