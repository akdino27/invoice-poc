using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByProductIdAsync(string productId)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<List<Product>> GetByProductIdsAsync(List<string> productIds)
        {
            if (productIds == null || productIds.Count == 0)
                return new List<Product>();

            return await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsAsync(
            Guid? vendorId,
            string? category,
            string? search,
            int skip,
            int take)
        {
            var query = _context.Products.AsQueryable();

            // Filter products that appear in invoices uploaded by this vendor
            if (vendorId.HasValue)
            {
                query = query.Where(p => _context.InvoiceLines
                    .Any(il => il.ProductId == p.ProductId &&
                               il.Invoice.UploadedByVendorId == vendorId.Value));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catLower = category.ToLower();
                query = query.Where(p => p.Category != null && p.Category.ToLower().Contains(catLower));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchLower) ||
                    p.ProductId.ToLower().Contains(searchLower));
            }

            return await query
                .OrderByDescending(p => p.TotalRevenue)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetProductCountAsync(Guid? vendorId, string? category, string? search)
        {
            var query = _context.Products.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(p => _context.InvoiceLines
                    .Any(il => il.ProductId == p.ProductId &&
                               il.Invoice.UploadedByVendorId == vendorId.Value));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catLower = category.ToLower();
                query = query.Where(p => p.Category != null && p.Category.ToLower().Contains(catLower));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchLower) ||
                    p.ProductId.ToLower().Contains(searchLower));
            }

            return await query.CountAsync();
        }

        public async Task<List<string>> GetVendorProductIdsAsync(Guid vendorId)
        {
            return await _context.InvoiceLines
                .Include(il => il.Invoice)
                .Where(il => il.Invoice.UploadedByVendorId == vendorId)
                .Select(il => il.ProductId)
                .Distinct()
                .ToListAsync();
        }

        // FIX: Rewritten to avoid "InvalidOperationException" in LINQ translation
        public async Task<List<(string Category, int ProductCount, decimal TotalRevenue)>> GetCategoriesAsync(Guid? vendorId)
        {
            var query = _context.Products.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(p => _context.InvoiceLines
                    .Any(il => il.ProductId == p.ProductId &&
                               il.Invoice.UploadedByVendorId == vendorId.Value));
            }

            // 1. Filter null categories
            // 2. Group by Category
            // 3. Project to Anonymous Type first (Supported by EF Core)
            var groups = await query
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(p => p.TotalRevenue)
                })
                .OrderByDescending(g => g.Revenue)
                .ToListAsync();

            // 4. Map to Tuple in memory
            return groups
                .Select(g => (g.Category!, g.Count, g.Revenue))
                .ToList();
        }

        public async Task<bool> CanVendorAccessProductAsync(string productId, Guid vendorId)
        {
            return await _context.InvoiceLines
                .Include(il => il.Invoice)
                .AnyAsync(il => il.ProductId == productId &&
                               il.Invoice.UploadedByVendorId == vendorId);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            return await Task.FromResult(product);
        }

        public Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}