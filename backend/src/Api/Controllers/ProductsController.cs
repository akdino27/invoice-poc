using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Api.Controllers
{
    // Controller for product catalog operations
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get all products with pagination
        [HttpGet]
        [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var query = _context.Products.AsQueryable();

            // Filter by category
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Contains(category));
            }

            // Search by product name or ID
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductId.Contains(search));
            }

            var total = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.TotalRevenue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
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
                })
                .ToListAsync();

            return Ok(new ProductListResponse
            {
                Products = products,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        // Get product by ID (GUID)
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _context.Products
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
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
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }

            return Ok(product);
        }

        // Get product by business ProductId
        [HttpGet("by-code/{productId}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductByProductId(string productId)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == productId)
                .Select(p => new ProductDto
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
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { Message = $"Product with code {productId} not found" });
            }

            return Ok(product);
        }
        // Get list of categories
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Products
                .Where(p => p.PrimaryCategory != null)
                .GroupBy(p => p.PrimaryCategory)
                .Select(g => new CategoryDto
                {
                    Category = g.Key!,
                    ProductCount = g.Count(),
                    TotalRevenue = g.Sum(p => p.TotalRevenue)
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToListAsync();

            return Ok(categories);
        }
    }

    public class CategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
