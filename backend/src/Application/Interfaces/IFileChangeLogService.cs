namespace invoice_v1.src.Application.Interfaces
{
    public interface IFileChangeLogService
    {
        Task<object> GetLogsAsync(
            Guid? vendorId,
            string? changeType,
            int page,
            int pageSize);

        Task<object?> GetLogByIdAsync(int id, Guid? vendorId);

        Task<object> GetLogStatsAsync(Guid? vendorId);
    }
}
