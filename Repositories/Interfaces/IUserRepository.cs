using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> IsEmailOrPhoneExistAsync(string email, string phoneNumber);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task AddUserAsync(User user);
    }
}