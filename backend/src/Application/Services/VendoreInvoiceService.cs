using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Services
{
    public class VendorInvoiceService : IVendorInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly ILogger<VendorInvoiceService> _logger;

        private readonly HashSet<string> _allowedMimeTypes = new()
        {
            "application/pdf",
            "image/jpeg",
            "image/png"
        };

        public VendorInvoiceService(
            ApplicationDbContext context,
            IGoogleDriveService googleDriveService,
            ILogger<VendorInvoiceService> logger)
        {
            _context = context;
            _googleDriveService = googleDriveService;
            _logger = logger;
        }

        public async Task<object> UploadInvoiceAsync(Guid vendorId, IFormFile file)
        {
            // ================================
            // 1. Validate Vendor
            // ================================

            var vendor = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == vendorId &&
                    !u.IsSoftDeleted);

            if (vendor == null)
                throw new InvalidOperationException("Vendor not found");

            if (vendor.Role != UserRole.Vendor)
                throw new InvalidOperationException("Only vendors can upload invoices");

            if (vendor.Status != UserStatus.Approved)
                throw new InvalidOperationException("Vendor is not approved");

            // ================================
            // 2. Validate File
            // ================================

            if (!_allowedMimeTypes.Contains(file.ContentType))
                throw new InvalidOperationException("Unsupported file type");

            // ================================
            // 3. Upload to Google Drive
            // ================================

            using var stream = file.OpenReadStream();

            var driveFile = await _googleDriveService.UploadFileAsync(
                file.FileName,
                file.ContentType,
                stream,
                vendor.Email);

            _logger.LogInformation(
                "Vendor {VendorId} uploaded invoice {FileName} to DriveFileId {DriveId}",
                vendorId,
                file.FileName,
                driveFile.Id);

            // ================================
            // 4. Return Response
            // ================================

            return new
            {
                message = "Invoice uploaded successfully",
                driveFileId = driveFile.Id,
                fileName = file.FileName
            };
        }
    }
}
