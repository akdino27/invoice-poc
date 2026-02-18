using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Repositories;
using invoice_v1.src.Services;
using Microsoft.AspNetCore.Http;

namespace invoice_v1.src.Application.Services
{
    public class VendorInvoiceService : IVendorInvoiceService
    {
        private readonly IGoogleDriveService _driveService;
        private readonly IUserRepository _userRepository;
        private readonly IFileChangeLogRepository _fileChangeLogRepository;
        private readonly ILogger<VendorInvoiceService> _logger;

        public VendorInvoiceService(
            IGoogleDriveService driveService,
            IUserRepository userRepository,
            IFileChangeLogRepository fileChangeLogRepository,
            ILogger<VendorInvoiceService> logger)
        {
            _driveService = driveService;
            _userRepository = userRepository;
            _fileChangeLogRepository = fileChangeLogRepository;
            _logger = logger;
        }

        public async Task<object> UploadInvoiceAsync(Guid vendorId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is required");
            }

            var vendor = await _userRepository.GetByIdAsync(vendorId);
            if (vendor == null || vendor.IsSoftDeleted)
            {
                throw new InvalidOperationException("Vendor not found");
            }

            var allowedMimeTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png"
            };

            if (!allowedMimeTypes.Contains(file.ContentType))
            {
                throw new ArgumentException(
                    $"File type {file.ContentType} is not allowed. " +
                    "Allowed types: PDF, JPG, JPEG, PNG");
            }

            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException(
                    $"File size {file.Length} bytes exceeds maximum allowed size of {maxFileSize} bytes");
            }

            DriveFileResult driveFile;
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    driveFile = await _driveService.UploadFileAsync(
                        fileName: file.FileName,
                        mimeType: file.ContentType,
                        stream: stream,
                        email: vendor.Email
                    );
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("permission"))
            {
                _logger.LogError(ex,
                    "Google Drive permission error for vendor {VendorId}",
                    vendorId);

                throw new InvalidOperationException(
                    "Google Drive is not properly configured. Please contact the administrator.");
            }

            var fileChangeLog = new FileChangeLog
            {
                FileName = driveFile.Name,
                FileId = driveFile.Id,
                ChangeType = "Upload",
                DetectedAt = DateTime.UtcNow,
                MimeType = file.ContentType,
                FileSize = file.Length,
                ModifiedBy = vendor.Email,
                GoogleDriveModifiedTime = DateTime.UtcNow,
                Processed = false,
                UploadedByVendorId = vendorId
            };

            await _fileChangeLogRepository.CreateAsync(fileChangeLog);
            await _fileChangeLogRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Vendor {VendorId} uploaded file {FileName} to Drive with ID {DriveFileId}",
                vendorId,
                file.FileName,
                driveFile.Id);

            return new
            {
                success = true,
                message = "File uploaded successfully",
                fileId = driveFile.Id,
                fileName = driveFile.Name,
                webViewLink = driveFile.WebViewLink
            };
        }
    }
}
