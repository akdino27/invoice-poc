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

        public async Task<(List<FileChangeLogDto> Logs, int Total)> GetLogsAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;
            var logs = await fileChangeLogRepository.GetAllAsync(skip, pageSize);
            var total = await fileChangeLogRepository.GetCountAsync();

            var logDtos = logs.Select(l => new FileChangeLogDto
            {
                Id = l.Id,  // Already Guid now
                FileName = l.FileName,
                FileId = l.FileId,
                ChangeType = l.ChangeType,
                DetectedAt = l.DetectedAt,
                MimeType = l.MimeType,
                FileSize = l.FileSize,
                ModifiedBy = l.ModifiedBy,
                GoogleDriveModifiedTime = l.GoogleDriveModifiedTime,
                ProcessedAt = l.ProcessedAt
            }).ToList();

            return (logDtos, total);
        }
    }
}
