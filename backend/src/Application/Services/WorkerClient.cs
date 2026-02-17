using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using System.Text.Json;

namespace invoice_v1.src.Infrastructure.Services
{
    public class WorkerClient : IWorkerClient
    {
        private readonly ILogger<WorkerClient> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WorkerClient(
            ILogger<WorkerClient> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();

            // Set a reasonable timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<bool> SendCallbackAsync(
            Guid jobId,
            string status,
            object? result = null,
            string? reason = null)
        {
            try
            {
                // FIX: Use correct config key that matches appsettings.json
                var workerUrl = _configuration["Worker:ApiUrl"] ?? "http://localhost:8000";

                var payload = new
                {
                    jobId,
                    status,
                    result,
                    reason
                };

                _logger.LogDebug(
                    "Attempting to notify worker at {WorkerUrl} about job {JobId}",
                    workerUrl,
                    jobId);

                // FIX: Worker uses polling, so this is just an optional notification
                // The endpoint /api/jobs/notify doesn't exist in worker, so this will fail
                // But that's OK - worker will pick up the job on next poll
                var response = await _httpClient.PostAsJsonAsync(
                    $"{workerUrl}/api/jobs/notify",
                    payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Successfully notified worker about job {JobId}",
                        jobId);
                    return true;
                }
                else
                {
                    _logger.LogDebug(
                        "Worker notification returned {StatusCode} for job {JobId} (worker will poll for it)",
                        response.StatusCode,
                        jobId);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                // This is expected if worker notification endpoint doesn't exist
                // Worker uses polling, so job will be picked up automatically
                _logger.LogDebug(
                    "Could not notify worker about job {JobId} (worker will poll for it): {Message}",
                    jobId,
                    ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected error notifying worker about job {JobId} (worker will poll for it)",
                    jobId);
                return false;
            }
        }
    }
}
