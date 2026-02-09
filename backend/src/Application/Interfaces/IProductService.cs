using invoice_v1.src.Api.Controllers;
using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductListResponse> GetProductsAsync(string? category, string? search, int page, int pageSize);
        Task<ProductDto?> GetProductByIdAsync(Guid id);
        Task<ProductDto?> GetProductByProductIdAsync(string productId);
        Task<List<CategoryDto>> GetCategoriesAsync();
    }
}
