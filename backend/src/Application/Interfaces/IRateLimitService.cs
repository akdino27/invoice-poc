namespace invoice_v1.src.Application.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> IsRateLimitedAsync(string key, int maxAttempts, TimeSpan window);
        Task IncrementAsync(string key, TimeSpan window);
        Task ResetAsync(string key);
        Task<int> GetAttemptsAsync(string key);
    }
}
