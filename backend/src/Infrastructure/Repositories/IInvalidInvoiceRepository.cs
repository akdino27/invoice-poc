using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IInvalidInvoiceRepository
    {
        Task<InvalidInvoice> CreateAsync(InvalidInvoice invalidInvoice);
        Task<InvalidInvoice?> GetByIdAsync(Guid id);
        Task<List<InvalidInvoice>> GetAllAsync(int skip, int take);
        Task<int> GetCountAsync();
    }
}
