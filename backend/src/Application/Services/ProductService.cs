using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<ProductListResponse> GetProductsAsync(
            Guid? vendorId,
            string? category,
            string? search,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;

            var total = await _productRepository.GetProductCountAsync(vendorId, category, search);
            var products = await _productRepository.GetProductsAsync(vendorId, category, search, skip, pageSize);

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

            _logger.LogInformation(
                "Retrieved {Count} products (page {Page}) for vendor {VendorId}",
                productDtos.Count,
                page,
                vendorId?.ToString() ?? "ALL");

            return new ProductListResponse
            {
                Products = productDtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id, Guid? vendorId)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return null;
            }

            // Check vendor access if vendorId is provided
            if (vendorId.HasValue)
            {
                var hasAccess = await _productRepository.CanVendorAccessProductAsync(
                    product.ProductId,
                    vendorId.Value);

                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "Vendor {VendorId} attempted to access product {ProductId} they don't own",
                        vendorId.Value,
                        product.ProductId);
                    return null;
                }
            }

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

        public async Task<ProductDto?> GetProductByProductIdAsync(string productId, Guid? vendorId)
        {
            var product = await _productRepository.GetByProductIdAsync(productId);

            if (product == null)
            {
                return null;
            }

            // Check vendor access if vendorId is provided
            if (vendorId.HasValue)
            {
                var hasAccess = await _productRepository.CanVendorAccessProductAsync(
                    productId,
                    vendorId.Value);

                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "Vendor {VendorId} attempted to access product {ProductId} they don't own",
                        vendorId.Value,
                        productId);
                    return null;
                }
            }

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

        public async Task<List<CategoryDto>> GetCategoriesAsync(Guid? vendorId)
        {
            var categories = await _productRepository.GetCategoriesAsync(vendorId);

            return categories.Select(c => new CategoryDto
            {
                Category = c.Category,
                ProductCount = c.ProductCount,
                TotalRevenue = c.TotalRevenue
            }).ToList();
        }

        public async Task<bool> CanVendorAccessProductAsync(string productId, Guid vendorId)
        {
            return await _productRepository.CanVendorAccessProductAsync(productId, vendorId);
        }
    }
}
