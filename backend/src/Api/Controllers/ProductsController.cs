using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// <summary>
    /// Product query endpoints with RBAC support.
    /// REFACTORED: Uses RbacActionFilter to eliminate duplicate code.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(RbacActionFilter))]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService productService;
        private readonly ILogger<ProductsController> logger;

        public ProductsController(
            IProductService productService,
            ILogger<ProductsController> logger)
        {
            this.productService = productService;
            this.logger = logger;
        }

        /// <summary>
        /// Gets products with RBAC filtering.
        /// Non-admins see only their own products.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var result = await productService.GetProductsAsync(
                category,
                search,
                page,
                pageSize,
                userEmail,
                isAdmin);

            return Ok(result);
        }

        /// <summary>
        /// Gets a product by internal ID with RBAC enforcement.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            try
            {
                var product = await productService.GetProductByIdAsync(id, userEmail, isAdmin);

                if (product == null)
                {
                    return NotFound(new { Message = $"Product with ID {id} not found" });
                }

                return Ok(product);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized access attempt by {UserEmail} for product {ProductId}", userEmail, id);
                return StatusCode(403, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a product by business ProductId with RBAC enforcement.
        /// For vendors, searches only within their own products.
        /// </summary>
        [HttpGet("by-code/{productId}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductByProductId(string productId)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            try
            {
                var product = await productService.GetProductByProductIdAsync(productId, userEmail, isAdmin);

                if (product == null)
                {
                    return NotFound(new { Message = $"Product with code {productId} not found" });
                }

                return Ok(product);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized access attempt by {UserEmail} for product code {ProductId}", userEmail, productId);
                return StatusCode(403, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets categories for the authenticated vendor.
        /// Admins get all categories.
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCategories()
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var categories = await productService.GetCategoriesAsync(userEmail, isAdmin);

            return Ok(categories);
        }
    }
}
