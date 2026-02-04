using invoice_v1.src.Api.Controllers;
using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository productRepository;
        private readonly ILogger<ProductService> logger;

        public ProductService(
            IProductRepository productRepository,
            ILogger<ProductService> logger)
        {
            this.productRepository = productRepository;
            this.logger = logger;
        }

        public async Task<ProductListResponse> GetProductsAsync(
            string? category,
            string? search,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;

            var products = await productRepository.GetProductsAsync(category, search, skip, pageSize);
            var total = await productRepository.GetProductCountAsync(category, search);

            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Category = p.Category,
                PrimaryCategory = p.PrimaryCategory,
                SecondaryCategory = p.SecondaryCategory,
                DefaultUnitRate = p.DefaultUnitRate,
                TotalQuantitySold = p.TotalQuantitySold,
                TotalRevenue = p.TotalRevenue,
                InvoiceCount = p.InvoiceCount,
                LastSoldDate = p.LastSoldDate,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return new ProductListResponse
            {
                Products = productDtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            var product = await productRepository.GetByIdAsync(id);
            if (product == null)
                return null;

            return new ProductDto
            {
                Id = product.Id,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Category = product.Category,
                PrimaryCategory = product.PrimaryCategory,
                SecondaryCategory = product.SecondaryCategory,
                DefaultUnitRate = product.DefaultUnitRate,
                TotalQuantitySold = product.TotalQuantitySold,
                TotalRevenue = product.TotalRevenue,
                InvoiceCount = product.InvoiceCount,
                LastSoldDate = product.LastSoldDate,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        public async Task<ProductDto?> GetProductByProductIdAsync(string productId)
        {
            var product = await productRepository.GetByProductIdAsync(productId);
            if (product == null)
                return null;

            return new ProductDto
            {
                Id = product.Id,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Category = product.Category,
                PrimaryCategory = product.PrimaryCategory,
                SecondaryCategory = product.SecondaryCategory,
                DefaultUnitRate = product.DefaultUnitRate,
                TotalQuantitySold = product.TotalQuantitySold,
                TotalRevenue = product.TotalRevenue,
                InvoiceCount = product.InvoiceCount,
                LastSoldDate = product.LastSoldDate,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            var categories = await productRepository.GetCategoriesAsync();

            return categories.Select(c => new CategoryDto
            {
                Category = c.Category,
                ProductCount = c.ProductCount,
                TotalRevenue = c.TotalRevenue
            }).ToList();
        }
    }
}
