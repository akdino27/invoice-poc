using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Services
{
    // Analytics service for insightful data retrieval
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProductSalesDto>> GetProductSalesByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? category = null)
        {
            var query = _context.InvoiceLines
                .Include(l => l.Invoice)
                .Include(l => l.Product)
                .Where(l => l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(l => l.Category != null && l.Category.Contains(category));
            }

            var results = await query
                .GroupBy(l => new
                {
                    l.ProductId,
                    l.ProductName,
                    l.Category
                })
                .Select(g => new ProductSalesDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category,
                    TotalQuantity = g.Sum(l => l.Quantity),
                    TotalRevenue = g.Sum(l => l.Amount),
                    InvoiceCount = g.Select(l => l.InvoiceId).Distinct().Count(),
                    AverageUnitRate = g.Average(l => l.UnitRate)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .ToListAsync();

            _logger.LogInformation(
                "ProductSalesByDateRange: {StartDate} to {EndDate}, Category: {Category}, Results: {Count}",
                startDate.ToShortDateString(),
                endDate.ToShortDateString(),
                category ?? "All",
                results.Count);

            return results;
        }

        public async Task<List<ProductTrendDto>> GetTrendingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int topN = 10)
        {
            var results = await _context.InvoiceLines
                .Include(l => l.Invoice)
                .Where(l => l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate)
                .GroupBy(l => new
                {
                    l.ProductId,
                    l.ProductName,
                    l.Category
                })
                .Select(g => new ProductTrendDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category,
                    TotalQuantity = g.Sum(l => l.Quantity),
                    TotalRevenue = g.Sum(l => l.Amount),
                    InvoiceCount = g.Count(),
                    GrowthRate = 0 // Can be computed by comparing to previous period
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(topN)
                .ToListAsync();

            // Assign ranks
            for (int i = 0; i < results.Count; i++)
            {
                results[i].Rank = i + 1;
            }

            _logger.LogInformation(
                "TrendingProducts: {StartDate} to {EndDate}, Top {TopN}, Found: {Count}",
                startDate.ToShortDateString(),
                endDate.ToShortDateString(),
                topN,
                results.Count);

            return results;
        }

        // Get sales by category
        public async Task<List<CategorySalesDto>> GetCategorySalesAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var results = await _context.InvoiceLines
                .Include(l => l.Invoice)
                .Where(l => l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate &&
                           l.Category != null)
                .GroupBy(l => l.Category)
                .Select(g => new CategorySalesDto
                {
                    Category = g.Key!,
                    ProductCount = g.Select(l => l.ProductId).Distinct().Count(),
                    TotalQuantity = g.Sum(l => l.Quantity),
                    TotalRevenue = g.Sum(l => l.Amount),
                    InvoiceCount = g.Select(l => l.InvoiceId).Distinct().Count(),
                    AverageOrderValue = g.Average(l => l.Amount)
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToListAsync();

            return results;
        }

        // Get time-series data for a specific product
        public async Task<List<ProductTimeSeriesDto>> GetProductTimeSeriesAsync(
            string productId,
            DateTime startDate,
            DateTime endDate,
            TimeGranularity granularity = TimeGranularity.Monthly)
        {
            var lineItems = await _context.InvoiceLines
                .Include(l => l.Invoice)
                .Where(l => l.ProductId == productId &&
                           l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate)
                .Select(l => new
                {
                    l.ProductId,
                    l.ProductName,
                    l.Quantity,
                    l.Amount,
                    InvoiceDate = l.Invoice.InvoiceDate!.Value
                })
                .ToListAsync();

            var grouped = lineItems
                .GroupBy(l => TruncateDate(l.InvoiceDate, granularity))
                .Select(g => new ProductTimeSeriesDto
                {
                    Period = g.Key,
                    ProductId = g.First().ProductId,
                    ProductName = g.First().ProductName,
                    Quantity = g.Sum(l => l.Quantity),
                    Revenue = g.Sum(l => l.Amount),
                    InvoiceCount = g.Count()
                })
                .OrderBy(p => p.Period)
                .ToList();

            return grouped;
        }

        private DateTime TruncateDate(DateTime date, TimeGranularity granularity)
        {
            return granularity switch
            {
                TimeGranularity.Daily => date.Date,
                TimeGranularity.Weekly => date.Date.AddDays(-(int)date.DayOfWeek),
                TimeGranularity.Monthly => new DateTime(date.Year, date.Month, 1),
                TimeGranularity.Quarterly => new DateTime(date.Year, ((date.Month - 1) / 3) * 3 + 1, 1),
                TimeGranularity.Yearly => new DateTime(date.Year, 1, 1),
                _ => date.Date
            };
        }
    }
}
