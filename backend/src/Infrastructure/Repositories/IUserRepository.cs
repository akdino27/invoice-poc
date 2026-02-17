using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetPendingVendorsAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<bool> AnyAdminExistsAsync();
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task<int> SaveChangesAsync();
    }
}
