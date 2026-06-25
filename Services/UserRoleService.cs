using Backend.Entities;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services;

public class UserRoleService : IUserRoleService
{
    private readonly UserManager<User> _userManager;

    public UserRoleService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public Task<IList<string>> GetRolesAsync(User user) => _userManager.GetRolesAsync(user);

    public async Task<string?> GetPrimaryRoleAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    public Task<bool> IsInRoleAsync(User user, string role) => _userManager.IsInRoleAsync(user, role);
}
