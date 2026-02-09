using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    // Service interface for invoice operations with RBAC support.
    public interface IInvoiceService
    {
 
        // Create or update invoice from worker callback.
        // Automatically creates vendor if first-time upload.
        Task<InvoiceDto> CreateOrUpdateInvoiceFromCallbackAsync(Guid jobId, object result);

        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, string userEmail, bool isAdmin = false);

        // Get invoice by Drive file ID with RBAC enforcement.
        Task<InvoiceDto?> GetInvoiceByFileIdAsync(string fileId, string userEmail, bool isAdmin = false);

        // Get all invoices for a specific vendor (RBAC enforced).
        // Admins can pass null for vendorEmail to get all invoices.
        Task<List<InvoiceDto>> GetInvoicesByVendorAsync(string? vendorEmail, int skip, int take, bool isAdmin = false);

        // Get invoice count for vendor.
        Task<int> GetInvoiceCountByVendorAsync(string? vendorEmail, bool isAdmin = false);
    }
}
