using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Services
{
    // Analytics service for insightful data retrieval with RBAC support.
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<AnalyticsService> logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<List<ProductSalesDto>> GetProductSalesByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? category = null,
            string? userEmail = null,
            bool isAdmin = false)
        {
            var query = context.InvoiceLines
                .Include(l => l.Invoice)
                .Include(l => l.Product)
                .Where(l => l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate);

            // RBAC filtering
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(l => l.Invoice.VendorEmail == userEmail);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(l => l.Category != null && l.Category.Contains(category));
            }

            var results = await query
                .GroupBy(l => new { l.ProductId, l.ProductName, l.Category })
                .Select(g => new ProductSalesDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category,
                    TotalQuantity = g.Sum(l => l.Quantity ?? 0),        //  FIX
                    TotalRevenue = g.Sum(l => l.Amount ?? 0),           //  FIX
                    InvoiceCount = g.Select(l => l.InvoiceId).Distinct().Count(),
                    AverageUnitRate = g.Average(l => l.UnitRate ?? 0)   //  FIX
                })
                .OrderByDescending(p => p.TotalQuantity)
                .ToListAsync();

            logger.LogInformation(
                "ProductSalesByDateRange: {StartDate} to {EndDate}, Category: {Category}, User: {UserEmail}, Results: {Count}",
                startDate.ToShortDateString(),
                endDate.ToShortDateString(),
                category ?? "All",
                userEmail ?? "Admin",
                results.Count);

            return results;
        }

        public async Task<List<ProductTrendDto>> GetTrendingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int topN = 10,
            string? userEmail = null,
            bool isAdmin = false)
        {
            var query = context.InvoiceLines
                .Include(l => l.Invoice)
                .Where(l => l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate);

            // RBAC filtering
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(l => l.Invoice.VendorEmail == userEmail);
            }

            var results = await query
                .GroupBy(l => new { l.ProductId, l.ProductName, l.Category })
                .Select(g => new ProductTrendDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category,
                    TotalQuantity = g.Sum(l => l.Quantity ?? 0),  
                    TotalRevenue = g.Sum(l => l.Amount ?? 0),   
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

            logger.LogInformation(
                "TrendingProducts: {StartDate} to {EndDate}, Top {TopN}, User: {UserEmail}, Found: {Count}",
                startDate.ToShortDateString(),
                endDate.ToShortDateString(),
                topN,
                userEmail ?? "Admin",
                results.Count);

            return results;
        }

        public async Task<List<CategorySalesDto>> GetCategorySalesAsync(
            DateTime startDate,
            DateTime endDate,
            string? userEmail = null,
            bool isAdmin = false)
        {
            var query = context.InvoiceLines
                .Include(l => l.Invoice)
                .Where(l => l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate &&
                           l.Category != null);

            // RBAC filtering
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(l => l.Invoice.VendorEmail == userEmail);
            }

            var results = await query
                .GroupBy(l => l.Category!)
                .Select(g => new CategorySalesDto
                {
                    Category = g.Key,
                    ProductCount = g.Select(l => l.ProductId).Distinct().Count(),
                    TotalQuantity = g.Sum(l => l.Quantity ?? 0),        //  FIX
                    TotalRevenue = g.Sum(l => l.Amount ?? 0),           //  FIX
                    InvoiceCount = g.Select(l => l.InvoiceId).Distinct().Count(),
                    AverageOrderValue = g.Average(l => l.Amount ?? 0)   //  FIX
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToListAsync();

            return results;
        }

        public async Task<List<ProductTimeSeriesDto>> GetProductTimeSeriesAsync(
            string productId,
            DateTime startDate,
            DateTime endDate,
            TimeGranularity granularity = TimeGranularity.Monthly,
            string? userEmail = null,
            bool isAdmin = false)
        {
            var query = context.InvoiceLines
                .Include(l => l.Invoice)
                .Where(l => l.ProductId == productId &&
                           l.Invoice.InvoiceDate.HasValue &&
                           l.Invoice.InvoiceDate >= startDate &&
                           l.Invoice.InvoiceDate <= endDate);

            // RBAC filtering
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(l => l.Invoice.VendorEmail == userEmail);
            }

            var lineItems = await query
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
                    Quantity = g.Sum(l => l.Quantity ?? 0),  
                    Revenue = g.Sum(l => l.Amount ?? 0), 
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
