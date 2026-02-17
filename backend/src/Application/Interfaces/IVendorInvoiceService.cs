using Microsoft.AspNetCore.Http;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IVendorInvoiceService
    {
        Task<object> UploadInvoiceAsync(Guid vendorId, IFormFile file);
    }
}
