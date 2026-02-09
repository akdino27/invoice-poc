using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet]
        [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await productService.GetProductsAsync(category, search, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }

            return Ok(product);
        }

        [HttpGet("by-code/{productId}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductByProductId(string productId)
        {
            var product = await productService.GetProductByProductIdAsync(productId);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with code {productId} not found" });
            }

            return Ok(product);
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await productService.GetCategoriesAsync();
            return Ok(categories);
        }
    }
}
