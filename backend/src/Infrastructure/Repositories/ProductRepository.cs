using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<ProductRepository> logger;

        public ProductRepository(
            ApplicationDbContext context,
            ILogger<ProductRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByProductIdAsync(string productId)
        {
            return await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<Product?> GetByVendorAndProductIdAsync(string vendorEmail, string productId)
        {
            return await context.Products
                .FirstOrDefaultAsync(p => p.VendorEmail == vendorEmail && p.ProductId == productId);
        }

        public async Task<List<Product>> GetProductsAsync(
            string? vendorEmail,
            string? category,
            string? search,
            int skip,
            int take,
            bool isAdmin = false)
        {
            var query = context.Products.AsNoTracking();

            // RBAC filtering
            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(vendorEmail))
                {
                    return new List<Product>();
                }
                query = query.Where(p => p.VendorEmail == vendorEmail);
            }
            else if (!string.IsNullOrWhiteSpace(vendorEmail))
            {
                query = query.Where(p => p.VendorEmail == vendorEmail);
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Contains(category));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductId.Contains(search) ||
                    (p.Category != null && p.Category.Contains(search)));
            }

            return await query
                .OrderByDescending(p => p.TotalRevenue)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetProductCountAsync(
            string? vendorEmail,
            string? category,
            string? search,
            bool isAdmin = false)
        {
            var query = context.Products.AsQueryable();

            // RBAC filtering
            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(vendorEmail))
                {
                    return 0;
                }
                query = query.Where(p => p.VendorEmail == vendorEmail);
            }
            else if (!string.IsNullOrWhiteSpace(vendorEmail))
            {
                query = query.Where(p => p.VendorEmail == vendorEmail);
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Contains(category));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductId.Contains(search) ||
                    (p.Category != null && p.Category.Contains(search)));
            }

            return await query.CountAsync();
        }

        public async Task<List<(string Category, int ProductCount, decimal TotalRevenue)>> GetCategoriesAsync(
            string? vendorEmail,
            bool isAdmin = false)
        {
            var query = context.Products.AsQueryable();

            // RBAC filtering
            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(vendorEmail))
                {
                    return new List<(string, int, decimal)>();
                }
                query = query.Where(p => p.VendorEmail == vendorEmail);
            }
            else if (!string.IsNullOrWhiteSpace(vendorEmail))
            {
                query = query.Where(p => p.VendorEmail == vendorEmail);
            }

            return await query
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category!)
                .Select(g => new ValueTuple<string, int, decimal>(
                    g.Key,
                    g.Count(),
                    g.Sum(p => p.TotalRevenue)
                ))
                .OrderByDescending(x => x.Item3)
                .ToListAsync();
        }
    }
}
