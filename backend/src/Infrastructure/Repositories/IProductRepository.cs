using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByProductIdAsync(string productId);

        Task<Product?> GetByVendorAndProductIdAsync(string vendorEmail, string productId);
        Task<List<Product>> GetProductsAsync(
            string? vendorEmail,
            string? category,
            string? search,
            int skip,
            int take,
            bool isAdmin = false);

        Task<int> GetProductCountAsync(
            string? vendorEmail,
            string? category,
            string? search,
            bool isAdmin = false);

        Task<List<(string Category, int ProductCount, decimal TotalRevenue)>> GetCategoriesAsync(
            string? vendorEmail,
            bool isAdmin = false);
    }
}
