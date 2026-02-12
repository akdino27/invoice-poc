// not needed still did this for best-practices
namespace invoice_v1.src.Application.Interfaces
{
    public interface IWorkerClient
    {
        Task<bool> SendCallbackAsync(Guid jobId, string status, object? result = null, string? reason = null);
    }
}
