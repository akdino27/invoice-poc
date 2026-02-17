using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.BackgroundServices
{
    public class JobRetryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobRetryService> _logger;
        // Poll every 30 seconds to catch scheduled retries
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public JobRetryService(
            IServiceProvider serviceProvider,
            ILogger<JobRetryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Retry Service started. Polling every {Seconds} seconds.", _interval.TotalSeconds);

            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessRetriesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during job retry polling cycle");
                }
            }
        }

        private async Task ProcessRetriesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();

            var now = DateTime.UtcNow;

            // Find jobs that are PENDING and Ready for Retry (NextRetryAt <= Now)
            var jobsToRetry = await context.JobQueues
                .Where(j => j.Status == nameof(JobStatus.PENDING) &&
                            j.NextRetryAt != null &&
                            j.NextRetryAt <= now)
                .ToListAsync(stoppingToken);

            if (jobsToRetry.Any())
            {
                _logger.LogInformation("Found {Count} jobs due for retry", jobsToRetry.Count);

                foreach (var job in jobsToRetry)
                {
                    // Execute the retry logic
                    await jobService.ProcessPendingJobAsync(job);
                }
            }
        }
    }
}
