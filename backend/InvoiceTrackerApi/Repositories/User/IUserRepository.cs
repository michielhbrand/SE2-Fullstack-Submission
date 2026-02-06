using UserModel = InvoiceTrackerApi.Models.User;

namespace InvoiceTrackerApi.Repositories.User;

/// <summary>
/// Repository interface for User data access
/// </summary>
public interface IUserRepository
{
    Task<UserModel?> GetByIdAsync(string id);
    Task<UserModel> AddAsync(UserModel user);
    Task UpdateAsync(UserModel user);
    Task<bool> ExistsAsync(string id);
}
