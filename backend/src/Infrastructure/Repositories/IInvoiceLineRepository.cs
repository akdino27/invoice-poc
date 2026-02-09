using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IInvoiceLineRepository
    {
        Task<InvoiceLine?> GetByIdAsync(Guid id);
        Task<List<InvoiceLine>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<List<InvoiceLine>> GetByProductIdAsync(string productId, int skip, int take);
        Task<List<InvoiceLine>> GetByCategoryAsync(string category, int skip, int take);
        Task<InvoiceLine> CreateAsync(InvoiceLine invoiceLine);
        Task UpdateAsync(InvoiceLine invoiceLine);
        Task DeleteAsync(Guid id);
        Task DeleteByInvoiceIdAsync(Guid invoiceId);
    }
}
