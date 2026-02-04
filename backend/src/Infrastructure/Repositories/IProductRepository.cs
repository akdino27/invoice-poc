using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByProductIdAsync(string productId);
        Task<List<Product>> GetProductsAsync(string? category, string? search, int skip, int take);
        Task<int> GetProductCountAsync(string? category, string? search);
        Task<List<(string Category, int ProductCount, decimal TotalRevenue)>> GetCategoriesAsync();
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
    }
}
