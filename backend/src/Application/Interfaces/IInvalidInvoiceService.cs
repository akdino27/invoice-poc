using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IInvalidInvoiceService
    {
        Task CreateInvalidInvoiceFromJobAsync(Guid jobId, string reason);
        Task<List<InvalidInvoiceDto>> GetAllAsync(int skip, int take);
        Task<int> GetCountAsync();
    }
}
