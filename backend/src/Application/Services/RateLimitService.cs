using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    public interface IRateLimitService
    {
        Task<bool> IsRateLimitedAsync(string key, int maxAttempts, TimeSpan window);
        Task IncrementAsync(string key, TimeSpan window);
        Task ResetAsync(string key);
        Task<int> GetAttemptsAsync(string key);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(IDistributedCache cache, ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsRateLimitedAsync(string key, int maxAttempts, TimeSpan window)
        {
            try
            {
                var data = await GetRateLimitDataAsync(key);
                if (data == null) return false;

                if (data.ResetTime <= DateTime.UtcNow)
                {
                    await ResetAsync(key);
                    return false;
                }

                if (data.Attempts >= maxAttempts)
                {
                    _logger.LogWarning("Rate limit exceeded for key {Key}: {Attempts}/{Max}", key, data.Attempts, maxAttempts);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for key {Key}", key);
                return false; // Fail open
            }
        }

        public async Task IncrementAsync(string key, TimeSpan window)
        {
            try
            {
                var data = await GetRateLimitDataAsync(key);

                if (data == null || data.ResetTime <= DateTime.UtcNow)
                {
                    data = new RateLimitData
                    {
                        Attempts = 1,
                        ResetTime = DateTime.UtcNow.Add(window)
                    };
                }
                else
                {
                    data.Attempts++;
                }

                await SetRateLimitDataAsync(key, data, window);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing rate limit for key {Key}", key);
            }
        }

        public async Task ResetAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting rate limit for key {Key}", key);
            }
        }

        public async Task<int> GetAttemptsAsync(string key)
        {
            try
            {
                var data = await GetRateLimitDataAsync(key);
                return data?.Attempts ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<RateLimitData?> GetRateLimitDataAsync(string key)
        {
            var json = await _cache.GetStringAsync(key);
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<RateLimitData>(json);
        }

        private async Task SetRateLimitDataAsync(string key, RateLimitData data, TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(data);
            var options = new DistributedCacheEntryOptions { AbsoluteExpiration = data.ResetTime };
            await _cache.SetStringAsync(key, json, options);
        }

        private class RateLimitData
        {
            public int Attempts { get; set; }
            public DateTime ResetTime { get; set; }
        }
    }
}
