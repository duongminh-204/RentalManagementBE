using Backend.Entities;

namespace Backend.Services.Interfaces;

public interface IUserRoleService
{
    Task<IList<string>> GetRolesAsync(User user);
    Task<string?> GetPrimaryRoleAsync(User user);
    Task<bool> IsInRoleAsync(User user, string role);
}
