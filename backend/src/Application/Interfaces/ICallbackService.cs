using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public class CallbackResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    public interface ICallbackService
    {
        Task<CallbackResult> ProcessCallbackAsync(CallbackRequest request);
    }
}
