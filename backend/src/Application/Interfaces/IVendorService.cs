using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Application.Interfaces
{
    
    // Service interface for vendor management operations.
    
    public interface IVendorService
    {
        
        // Get or create vendor by email.
        // Auto-creates vendor if doesn't exist (for first-time uploaders).
        
        Task<Vendor> GetOrCreateVendorAsync(string email, string? displayName = null);

        
        // Get vendor by email.
        
        Task<VendorDto?> GetVendorByEmailAsync(string email);

        
        // Get all vendors (admin only).
        
        Task<List<VendorDto>> GetAllVendorsAsync(int skip, int take);

        
        // Get vendor count (admin only).
        
        Task<int> GetVendorCountAsync();

        
        // Update vendor's last activity timestamp.
        // Called after successful invoice upload.
        
        Task UpdateVendorActivityAsync(string email);
    }
}
