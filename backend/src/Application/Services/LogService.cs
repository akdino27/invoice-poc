using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    public class LogService : ILogService
    {
        private readonly IFileChangeLogRepository fileChangeLogRepository;
        private readonly ILogger<LogService> logger;

        public LogService(
            IFileChangeLogRepository fileChangeLogRepository,
            ILogger<LogService> logger)
        {
            this.fileChangeLogRepository = fileChangeLogRepository;
            this.logger = logger;
        }

        public async Task<(List<FileChangeLogDto> Logs, int Total)> GetLogsAsync(
            int page,
            int pageSize,
            string? userEmail = null,
            bool isAdmin = false)
        {
            var skip = (page - 1) * pageSize;

            // RBAC: Filter by ModifiedBy for non-admins
            var vendorEmailFilter = isAdmin ? null : userEmail;

            var (logs, total) = await fileChangeLogRepository.GetLogsAsync(
                skip,
                pageSize,
                vendorEmailFilter);

            var dtos = logs.Select(log => new FileChangeLogDto
            {
                Id = log.Id,
                FileName = log.FileName,
                FileId = log.FileId,
                ChangeType = log.ChangeType,
                DetectedAt = log.DetectedAt,
                MimeType = log.MimeType,
                FileSize = log.FileSize,
                ModifiedBy = log.ModifiedBy,
                GoogleDriveModifiedTime = log.GoogleDriveModifiedTime,
                ProcessedAt = log.ProcessedAt
            }).ToList();

            return (dtos, total);
        }
    }
}
