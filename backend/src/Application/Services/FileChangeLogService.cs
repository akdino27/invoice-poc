using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    public class FileChangeLogService : IFileChangeLogService
    {
        private readonly IFileChangeLogRepository _fileChangeLogRepository;
        private readonly ILogger<FileChangeLogService> _logger;

        public FileChangeLogService(
            IFileChangeLogRepository fileChangeLogRepository,
            ILogger<FileChangeLogService> logger)
        {
            _fileChangeLogRepository = fileChangeLogRepository;
            _logger = logger;
        }

        public async Task<object> GetLogsAsync(
            Guid? vendorId,
            string? changeType,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;

            var total = await _fileChangeLogRepository.GetLogCountAsync(vendorId, changeType);
            var logs = await _fileChangeLogRepository.GetLogsAsync(vendorId, changeType, skip, pageSize);

            _logger.LogInformation(
                "Retrieved {Count} file change logs for vendor {VendorId}",
                logs.Count,
                vendorId?.ToString() ?? "ALL");

            return new
            {
                logs = logs.Select(l => new
                {
                    l.Id,
                    l.FileName,
                    l.FileId,
                    l.ChangeType,
                    l.DetectedAt,
                    l.MimeType,
                    l.FileSize,
                    l.ModifiedBy,
                    l.GoogleDriveModifiedTime,
                    l.Processed,
                    l.ProcessedAt,
                    l.UploadedByVendorId
                }),
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<object?> GetLogByIdAsync(int id, Guid? vendorId)
        {
            var log = await _fileChangeLogRepository.GetByIdAsync(id);

            if (log == null)
            {
                return null;
            }

            // Check vendor access
            if (vendorId.HasValue && log.UploadedByVendorId != vendorId.Value)
            {
                _logger.LogWarning(
                    "Vendor {VendorId} attempted to access log {LogId} for file owned by {OwnerId}",
                    vendorId.Value,
                    id,
                    log.UploadedByVendorId);
                return null;
            }

            return new
            {
                log.Id,
                log.FileName,
                log.FileId,
                log.ChangeType,
                log.DetectedAt,
                log.MimeType,
                log.FileSize,
                log.ModifiedBy,
                log.GoogleDriveModifiedTime,
                log.Processed,
                log.ProcessedAt,
                log.UploadedByVendorId
            };
        }

        public async Task<object> GetLogStatsAsync(Guid? vendorId)
        {
            var stats = await _fileChangeLogRepository.GetLogStatsAsync(vendorId);
            var total = await _fileChangeLogRepository.GetLogCountAsync(vendorId, null);

            var totalProcessed = stats.Sum(s => s.Processed);

            return new
            {
                totalFiles = total,
                totalProcessed,
                totalPending = total - totalProcessed,
                byChangeType = stats.Select(s => new
                {
                    changeType = s.ChangeType,
                    count = s.Count,
                    processed = s.Processed,
                    pending = s.Pending
                })
            };
        }
    }
}
