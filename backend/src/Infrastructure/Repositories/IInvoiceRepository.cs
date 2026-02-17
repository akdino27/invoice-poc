using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id, bool includeLineItems = true);
        Task<Invoice?> GetByFileIdAsync(string fileId, bool includeLineItems = true);
        Task<List<Invoice>> GetInvoicesAsync(Guid? vendorId, int skip, int take);
        Task<int> GetInvoiceCountAsync(Guid? vendorId);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteLineItemsAsync(IEnumerable<InvoiceLine> lineItems);
        Task<int> SaveChangesAsync();
    }
}
