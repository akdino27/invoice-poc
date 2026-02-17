using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByProductIdAsync(string productId);
        Task<List<Product>> GetByProductIdsAsync(List<string> productIds); // FIX: Added
        Task<List<Product>> GetProductsAsync(
            Guid? vendorId,
            string? category,
            string? search,
            int skip,
            int take);
        Task<int> GetProductCountAsync(Guid? vendorId, string? category, string? search);
        Task<List<string>> GetVendorProductIdsAsync(Guid vendorId);
        Task<List<(string Category, int ProductCount, decimal TotalRevenue)>> GetCategoriesAsync(Guid? vendorId);
        Task<bool> CanVendorAccessProductAsync(string productId, Guid vendorId);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task<int> SaveChangesAsync();
    }
}
