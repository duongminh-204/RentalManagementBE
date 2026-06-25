using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RentalManagementDb _context;

        public UserRepository(RentalManagementDb context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> IsEmailOrPhoneExistAsync(string email, string phoneNumber)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email || u.PhoneNumber == phoneNumber);
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> IsEmailOrPhoneTakenByOtherAsync(string? email, string? phoneNumber, int excludeUserId)
        {
            return await _context.Users.AnyAsync(u =>
                u.Id != excludeUserId &&
                ((email != null && u.Email == email) || (phoneNumber != null && u.PhoneNumber == phoneNumber)));
        }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
