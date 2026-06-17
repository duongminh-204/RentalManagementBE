using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> IsEmailOrPhoneExistAsync(string email, string phoneNumber);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task AddUserAsync(User user);
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> IsEmailOrPhoneTakenByOtherAsync(string? email, string? phoneNumber, int excludeUserId);
        Task<int> SaveChangesAsync();
    }
}