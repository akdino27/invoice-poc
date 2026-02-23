using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    public class InvalidInvoiceService : IInvalidInvoiceService
    {
        private readonly IInvalidInvoiceRepository _invalidInvoiceRepository;
        private readonly IFileChangeLogRepository _fileChangeLogRepository;
        private readonly ILogger<InvalidInvoiceService> _logger;

        public InvalidInvoiceService(
            IInvalidInvoiceRepository invalidInvoiceRepository,
            IFileChangeLogRepository fileChangeLogRepository,
            ILogger<InvalidInvoiceService> logger)
        {
            _invalidInvoiceRepository = invalidInvoiceRepository;
            _fileChangeLogRepository = fileChangeLogRepository;
            _logger = logger;
        }

        public async Task<object> GetInvalidInvoicesAsync(int page, int pageSize, Guid? vendorId)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // 1. Extraction failures from InvalidInvoice table (JobId is set)
            var (extractionFailures, extractionTotal) = await _invalidInvoiceRepository
                .GetInvalidInvoicesAsync(page, pageSize, vendorId);

            var extractionDtos = extractionFailures.Select(i => new InvalidInvoiceDto
            {
                Id = i.Id.ToString(),
                FileId = i.FileId,
                FileName = i.FileName,
                VendorId = i.VendorId,
                JobId = i.JobId,
                Reason = i.Reason?.RootElement.ToString(),
                CreatedAt = i.CreatedAt,
                Type = "ExtractionFailure"
            });

            // 2. Security rejections from FileChangeLog table (SecurityStatus == Unhealthy)
            var (securityRejections, securityTotal) = await _fileChangeLogRepository
                .GetUnhealthyLogsAsync(page, pageSize, vendorId);

            var securityDtos = securityRejections.Select(l => new InvalidInvoiceDto
            {
                Id = $"sec_{l.Id}",
                FileId = l.FileId,
                FileName = l.FileName,
                VendorId = l.UploadedByVendorId,
                JobId = null,
                Reason = l.SecurityFailReason,
                CreatedAt = l.DetectedAt,
                Type = "SecurityViolation"
            });

            // 3. Merge, sort by CreatedAt descending, and paginate
            var merged = extractionDtos
                .Concat(securityDtos)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            var totalCount = extractionTotal + securityTotal;

            _logger.LogInformation(
                "Retrieved {Count} invalid invoices ({Extraction} extraction + {Security} security, page {Page}) for vendor {VendorId}",
                merged.Count, extractionTotal, securityTotal, page, vendorId?.ToString() ?? "ALL");

            return new
            {
                Data = merged,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        private class InvalidInvoiceDto
        {
            public string Id { get; set; } = string.Empty;
            public string? FileId { get; set; }
            public string? FileName { get; set; }
            public Guid? VendorId { get; set; }
            public Guid? JobId { get; set; }
            public string? Reason { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Type { get; set; } = string.Empty;
        }
    }
}
