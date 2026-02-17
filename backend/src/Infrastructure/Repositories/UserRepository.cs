using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsSoftDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<User>> GetPendingVendorsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Vendor &&
                           u.Status == UserStatus.Pending &&
                           !u.IsSoftDeleted)
                .OrderBy(u => u.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email.ToLower());
        }

        public async Task<bool> AnyAdminExistsAsync()
        {
            return await _context.Users
                .AnyAsync(u => u.Role == UserRole.Admin && !u.IsSoftDeleted);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            return user;
        }

        public Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
