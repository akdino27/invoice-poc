using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext context;

        public ProductRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await context.Products.FindAsync(id);
        }

        public async Task<Product?> GetByProductIdAsync(string productId)
        {
            return await context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<List<Product>> GetProductsAsync(string? category, string? search, int skip, int take)
        {
            var query = context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductId.Contains(search));
            }

            return await query
                .OrderByDescending(p => p.TotalRevenue)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetProductCountAsync(string? category, string? search)
        {
            var query = context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductId.Contains(search));
            }

            return await query.CountAsync();
        }

        public async Task<List<(string Category, int ProductCount, decimal TotalRevenue)>> GetCategoriesAsync()
        {
            return await context.Products
                .Where(p => p.PrimaryCategory != null)
                .GroupBy(p => p.PrimaryCategory)
                .Select(g => new ValueTuple<string, int, decimal>(
                    g.Key!,
                    g.Count(),
                    g.Sum(p => p.TotalRevenue)
                ))
                .OrderByDescending(c => c.Item3)
                .ToListAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            context.Entry(product).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
    }
}
