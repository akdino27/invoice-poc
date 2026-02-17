using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IInvalidInvoiceRepository
    {
        Task<(List<InvalidInvoice> Data, int Total)> GetInvalidInvoicesAsync(int page, int pageSize, Guid? vendorId);
        Task CreateAsync(InvalidInvoice invalidInvoice);
        Task DeleteByJobIdAsync(Guid jobId);
        Task SaveChangesAsync();
    }
}
