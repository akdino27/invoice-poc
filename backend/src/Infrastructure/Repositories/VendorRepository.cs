using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<VendorRepository> logger;

        public VendorRepository(
            ApplicationDbContext context,
            ILogger<VendorRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<Vendor?> GetByEmailAsync(string email)
        {
            return await context.Vendors
                .FirstOrDefaultAsync(v => v.Email == email);
        }

        public async Task<List<Vendor>> GetAllAsync(int skip, int take)
        {
            return await context.Vendors
                .OrderByDescending(v => v.LastActivityAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await context.Vendors.CountAsync();
        }

        public async Task<Vendor> CreateAsync(Vendor vendor)
        {
            context.Vendors.Add(vendor);
            await context.SaveChangesAsync();
            logger.LogInformation("Created vendor: {Email}", vendor.Email);
            return vendor;
        }

        public async Task UpdateAsync(Vendor vendor)
        {
            context.Vendors.Update(vendor);
            await context.SaveChangesAsync();
            logger.LogDebug("Updated vendor: {Email}", vendor.Email);
        }

        public async Task<bool> ExistsAsync(string email)
        {
            return await context.Vendors.AnyAsync(v => v.Email == email);
        }
    }
}
