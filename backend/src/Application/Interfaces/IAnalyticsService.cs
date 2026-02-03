using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    // Service for analytics and insights queries
    public interface IAnalyticsService
    {
        // Get products sold in a date range, optionally filtered by category
        // like "How many Tables were sold from June 2012 to September 2012?"
        Task<List<ProductSalesDto>> GetProductSalesByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? category = null);
        // Get trending products in a time period
        // like "Which product is most trendy in the past month?"
        Task<List<ProductTrendDto>> GetTrendingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int topN = 10);

        // Get category-level sales analytics
        Task<List<CategorySalesDto>> GetCategorySalesAsync(
            DateTime startDate,
            DateTime endDate);

        // Get product sales over time (time series)
        Task<List<ProductTimeSeriesDto>> GetProductTimeSeriesAsync(
            string productId,
            DateTime startDate,
            DateTime endDate,
            TimeGranularity granularity = TimeGranularity.Monthly);
    }

    public enum TimeGranularity
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }
}
