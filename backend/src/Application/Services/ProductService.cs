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
            int pageSize,
            string userEmail,
            bool isAdmin = false)
        {
            var skip = (page - 1) * pageSize;

            var vendorEmail = isAdmin ? null : userEmail;

            var products = await productRepository.GetProductsAsync(
                vendorEmail,
                category,
                search,
                skip,
                pageSize,
                isAdmin);

            var total = await productRepository.GetProductCountAsync(
                vendorEmail,
                category,
                search,
                isAdmin);

            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                VendorEmail = p.VendorEmail,
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

            logger.LogInformation(
                "Retrieved {Count} products for user {UserEmail} (Admin: {IsAdmin})",
                productDtos.Count,
                userEmail,
                isAdmin);

            return new ProductListResponse
            {
                Products = productDtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id, string userEmail, bool isAdmin = false)
        {
            var product = await productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return null;
            }

            // RBAC enforcement
            if (!isAdmin && product.VendorEmail != userEmail)
            {
                throw new UnauthorizedAccessException(
                    $"User {userEmail} is not authorized to access product {id}");
            }

            return new ProductDto
            {
                Id = product.Id,
                VendorEmail = product.VendorEmail,
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

        public async Task<ProductDto?> GetProductByProductIdAsync(
            string productId,
            string userEmail,
            bool isAdmin = false)
        {
            var product = isAdmin
                ? await productRepository.GetByProductIdAsync(productId)
                : await productRepository.GetByVendorAndProductIdAsync(userEmail, productId);

            if (product == null)
            {
                return null;
            }

            // Additional RBAC check for admin accessing specific product
            if (!isAdmin && product.VendorEmail != userEmail)
            {
                throw new UnauthorizedAccessException(
                    $"User {userEmail} is not authorized to access product {productId}");
            }

            return new ProductDto
            {
                Id = product.Id,
                VendorEmail = product.VendorEmail,
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

        public async Task<List<CategoryDto>> GetCategoriesAsync(string userEmail, bool isAdmin = false)
        {
            var vendorEmail = isAdmin ? null : userEmail;

            var categories = await productRepository.GetCategoriesAsync(vendorEmail, isAdmin);

            return categories.Select(c => new CategoryDto
            {
                Category = c.Category,
                ProductCount = c.ProductCount,
                TotalRevenue = c.TotalRevenue
            }).ToList();
        }
    }
}
