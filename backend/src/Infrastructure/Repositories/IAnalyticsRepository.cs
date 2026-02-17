using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Domain.Enums;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IAnalyticsRepository
    {
        Task<List<ProductSalesDto>> GetProductSalesByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? category,
            Guid? vendorId);

        Task<List<ProductTrendDto>> GetTrendingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int topN,
            Guid? vendorId);

        Task<List<CategorySalesDto>> GetCategorySalesAsync(
            DateTime startDate,
            DateTime endDate,
            Guid? vendorId);

        Task<List<(DateTime InvoiceDate, string ProductId, string ProductName, decimal Quantity, decimal Amount)>>
            GetProductTimeSeriesDataAsync(
                string productId,
                DateTime startDate,
                DateTime endDate,
                Guid? vendorId);
    }
}
