using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductListResponse> GetProductsAsync(
            Guid? vendorId,
            string? category,
            string? search,
            int page,
            int pageSize);

        Task<ProductDto?> GetProductByIdAsync(Guid id, Guid? vendorId);

        Task<ProductDto?> GetProductByProductIdAsync(string productId, Guid? vendorId);

        Task<List<CategoryDto>> GetCategoriesAsync(Guid? vendorId);

        Task<bool> CanVendorAccessProductAsync(string productId, Guid vendorId);
    }
}
