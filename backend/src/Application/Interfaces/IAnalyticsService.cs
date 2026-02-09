using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    /// <summary>
    /// Service for analytics and insights queries with RBAC support.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Get products sold in a date range, optionally filtered by category.
        /// Example: "How many Tables were sold from June 2012 to September 2012?"
        /// </summary>
        Task<List<ProductSalesDto>> GetProductSalesByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? category = null,
            string? userEmail = null,
            bool isAdmin = false);

        /// <summary>
        /// Get trending products in a time period.
        /// Example: "Which product is most trendy in the past month?"
        /// </summary>
        Task<List<ProductTrendDto>> GetTrendingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int topN = 10,
            string? userEmail = null,
            bool isAdmin = false);

        /// <summary>
        /// Get category-level sales analytics.
        /// </summary>
        Task<List<CategorySalesDto>> GetCategorySalesAsync(
            DateTime startDate,
            DateTime endDate,
            string? userEmail = null,
            bool isAdmin = false);

        /// <summary>
        /// Get product sales over time (time series).
        /// </summary>
        Task<List<ProductTimeSeriesDto>> GetProductTimeSeriesAsync(
            string productId,
            DateTime startDate,
            DateTime endDate,
            TimeGranularity granularity = TimeGranularity.Monthly,
            string? userEmail = null,
            bool isAdmin = false);
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
