using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateOrUpdateInvoiceFromCallbackAsync(Guid jobId, object result);

        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id);

        Task<InvoiceDto?> GetInvoiceByFileIdAsync(string fileId);

        Task<(List<InvoiceDto> Invoices, int Total)> GetInvoicesAsync(
            Guid? vendorId,
            int page,
            int pageSize);
    }
}
