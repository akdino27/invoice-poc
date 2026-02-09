using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    
    // Service interface for product operations with RBAC support.
    
    public interface IProductService
    {
        
        // Get products with RBAC filtering.
        // Non-admins only see their own products.
        
        Task<ProductListResponse> GetProductsAsync(
            string? category,
            string? search,
            int page,
            int pageSize,
            string userEmail,
            bool isAdmin = false);

        
        // Get product by internal ID with RBAC enforcement.
        
        Task<ProductDto?> GetProductByIdAsync(Guid id, string userEmail, bool isAdmin = false);

        
        // Get product by business ProductId (scoped to vendor).
        // For non-admins, searches within their own products only.
        
        Task<ProductDto?> GetProductByProductIdAsync(string productId, string userEmail, bool isAdmin = false);

        
        // Get categories for vendor's products.
        // Admins get all categories.
        
        Task<List<CategoryDto>> GetCategoriesAsync(string userEmail, bool isAdmin = false);
    }
}
