using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IInvalidInvoiceService
    {
        Task CreateInvalidInvoiceFromJobAsync(Guid jobId, string reason);

        Task<List<InvalidInvoiceDto>> GetAllAsync(
            int skip,
            int take,
            string? userEmail = null,
            bool isAdmin = false);

        Task<int> GetCountAsync(string? userEmail = null, bool isAdmin = false);
    }
}
