using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/analytics")]  // FIXED: Lowercase route
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

        [HttpGet("products/sales")]
        [ProducesResponseType(typeof(List<ProductSalesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductSales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? category = null)
        {
            if (startDate > endDate)
            {
                return BadRequest(new { error = "startDate cannot be after endDate" });
            }

            if (endDate > DateTime.UtcNow.AddDays(1))
            {
                return BadRequest(new { error = "endDate cannot be in the future" });
            }

            var vendorId = GetVendorIdIfVendor();

            var results = await _analyticsService.GetProductSalesByDateRangeAsync(
                startDate,
                endDate,
                category,
                vendorId);

            return Ok(results);
        }

        [HttpGet("products/trending")]
        [ProducesResponseType(typeof(List<ProductTrendDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrendingProducts(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int topN = 10)
        {
            if (startDate > endDate)
            {
                return BadRequest(new { error = "startDate cannot be after endDate" });
            }

            if (endDate > DateTime.UtcNow.AddDays(1))
            {
                return BadRequest(new { error = "endDate cannot be in the future" });
            }

            if (topN < 1)
            {
                return BadRequest(new { error = "topN must be at least 1" });
            }

            if (topN > 100)
            {
                return BadRequest(new { error = "topN cannot exceed 100" });
            }

            var vendorId = GetVendorIdIfVendor();

            var results = await _analyticsService.GetTrendingProductsAsync(
                startDate,
                endDate,
                topN,
                vendorId);

            return Ok(results);
        }

        [HttpGet("categories/sales")]
        [ProducesResponseType(typeof(List<CategorySalesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategorySales(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest(new { error = "startDate cannot be after endDate" });
            }

            if (endDate > DateTime.UtcNow.AddDays(1))
            {
                return BadRequest(new { error = "endDate cannot be in the future" });
            }

            var vendorId = GetVendorIdIfVendor();

            var results = await _analyticsService.GetCategorySalesAsync(
                startDate,
                endDate,
                vendorId);

            return Ok(results);
        }

        [HttpGet("products/{productId}/timeseries")]
        [ProducesResponseType(typeof(List<ProductTimeSeriesDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductTimeSeries(
            string productId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] TimeGranularity granularity = TimeGranularity.Monthly)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return BadRequest(new { error = "productId is required" });
            }

            if (startDate > endDate)
            {
                return BadRequest(new { error = "startDate cannot be after endDate" });
            }

            if (endDate > DateTime.UtcNow.AddDays(1))
            {
                return BadRequest(new { error = "endDate cannot be in the future" });
            }

            var vendorId = GetVendorIdIfVendor();

            var results = await _analyticsService.GetProductTimeSeriesAsync(
                productId,
                startDate,
                endDate,
                granularity,
                vendorId);

            return Ok(results);
        }

        private Guid? GetVendorIdIfVendor()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == UserRole.Vendor.ToString())
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdClaim, out var vendorId))
                {
                    _logger.LogDebug("Analytics request from vendor {VendorId}", vendorId);
                    return vendorId;
                }
            }

            _logger.LogDebug("Analytics request from admin - no vendor filter");
            return null;
        }
    }
}
