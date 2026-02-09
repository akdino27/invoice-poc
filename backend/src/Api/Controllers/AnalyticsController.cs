using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// <summary>
    /// Analytics and insights endpoints with RBAC support.
    /// REFACTORED: Uses RbacActionFilter to eliminate duplicate code.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(RbacActionFilter))]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService analyticsService;
        private readonly ILogger<AnalyticsController> logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            ILogger<AnalyticsController> logger)
        {
            this.analyticsService = analyticsService;
            this.logger = logger;
        }

        /// <summary>
        /// Get products sold in a date range (RBAC filtered).
        /// </summary>
        [HttpGet("products/sales")]
        [ProducesResponseType(typeof(List<ProductSalesDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProductSales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? category = null)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var results = await analyticsService.GetProductSalesByDateRangeAsync(
                startDate,
                endDate,
                category,
                userEmail,
                isAdmin);

            return Ok(results);
        }

        /// <summary>
        /// Get trending products (RBAC filtered).
        /// </summary>
        [HttpGet("products/trending")]
        [ProducesResponseType(typeof(List<ProductTrendDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTrendingProducts(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int topN = 10)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var results = await analyticsService.GetTrendingProductsAsync(
                startDate,
                endDate,
                topN,
                userEmail,
                isAdmin);

            return Ok(results);
        }

        /// <summary>
        /// Get category sales (RBAC filtered).
        /// </summary>
        [HttpGet("categories/sales")]
        [ProducesResponseType(typeof(List<CategorySalesDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCategorySales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var results = await analyticsService.GetCategorySalesAsync(
                startDate,
                endDate,
                userEmail,
                isAdmin);

            return Ok(results);
        }

        /// <summary>
        /// Get time-series for a product (RBAC filtered).
        /// </summary>
        [HttpGet("products/{productId}/timeseries")]
        [ProducesResponseType(typeof(List<ProductTimeSeriesDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProductTimeSeries(
            string productId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] TimeGranularity granularity = TimeGranularity.Monthly)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var results = await analyticsService.GetProductTimeSeriesAsync(
                productId,
                startDate,
                endDate,
                granularity,
                userEmail,
                isAdmin);

            return Ok(results);
        }
    }
}
