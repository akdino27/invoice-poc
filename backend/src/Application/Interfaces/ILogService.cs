using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface ILogService
    {
        Task<(List<FileChangeLogDto> Logs, int Total)> GetLogsAsync(int page, int pageSize);
    }
}
