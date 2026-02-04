using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id, bool includeLineItems = false);
        Task<Invoice?> GetByFileIdAsync(string fileId, bool includeLineItems = false);
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, bool includeLineItems = false);
        Task<List<Invoice>> GetAllAsync(int skip, int take, bool includeLineItems = false);
        Task<List<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeLineItems = false);
        Task<List<Invoice>> GetByVendorAsync(string vendorName, int skip, int take);
        Task<int> GetCountAsync();
        Task<Invoice> CreateAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(Guid id);
    }
}
