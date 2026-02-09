using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IVendorRepository
    {
        Task<Vendor?> GetByEmailAsync(string email);
        Task<List<Vendor>> GetAllAsync(int skip, int take);
        Task<int> GetCountAsync();
        Task<Vendor> CreateAsync(Vendor vendor);
        Task UpdateAsync(Vendor vendor);
        Task<bool> ExistsAsync(string email);
    }
}
