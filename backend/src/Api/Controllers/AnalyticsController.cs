using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    // Analytics and insights endpoints
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        // Get products sold in a date range
        [HttpGet("products/sales")]
        [ProducesResponseType(typeof(List<ProductSalesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductSales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? category = null)
        {
            var results = await _analyticsService.GetProductSalesByDateRangeAsync(
                startDate,
                endDate,
                category);

            return Ok(results);
        }

        // Get trending products
        [HttpGet("products/trending")]
        [ProducesResponseType(typeof(List<ProductTrendDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrendingProducts(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int topN = 10)
        {
            var results = await _analyticsService.GetTrendingProductsAsync(
                startDate,
                endDate,
                topN);

            return Ok(results);
        }

        // Get category sales
        [HttpGet("categories/sales")]
        [ProducesResponseType(typeof(List<CategorySalesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategorySales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var results = await _analyticsService.GetCategorySalesAsync(startDate, endDate);
            return Ok(results);
        }

        // Get time-series for a product
        [HttpGet("products/{productId}/timeseries")]
        [ProducesResponseType(typeof(List<ProductTimeSeriesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductTimeSeries(
            string productId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] TimeGranularity granularity = TimeGranularity.Monthly)
        {
            var results = await _analyticsService.GetProductTimeSeriesAsync(
                productId,
                startDate,
                endDate,
                granularity);

            return Ok(results);
        }
    }
}
