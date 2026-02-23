using invoice_v1.src.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IVendorInvoiceService
    {
        Task<UploadResult> UploadInvoiceAsync(Guid vendorId, IFormFile file);
    }
}
