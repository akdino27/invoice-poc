using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    // Service interface for invoice operations.
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateOrUpdateInvoiceFromCallbackAsync(Guid jobId, object result);
        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id);
        Task<InvoiceDto?> GetInvoiceByFileIdAsync(string fileId);
    }
}
