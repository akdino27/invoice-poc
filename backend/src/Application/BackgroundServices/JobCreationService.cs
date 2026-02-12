using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.BackgroundServices
{
    // Background service that periodically checks for unprocessed FileChangeLogs
    // and creates corresponding JobQueue entries.
    // This service owns the job creation logic.
    public class JobCreationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobCreationService> _logger;
        private readonly TimeSpan _interval;
        private const int BatchSize = 50;

        public JobCreationService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<JobCreationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Read interval from configuration (default: 30 seconds)
            var intervalSeconds = configuration.GetValue<int>("JobCreation:IntervalSeconds", 30);
            _interval = TimeSpan.FromSeconds(intervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Creation Service starting. Interval: {Interval}", _interval);

            // Wait a bit before first run to let the app fully initialize
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            using var timer = new PeriodicTimer(_interval);

            try
            {
                // Run first check immediately
                await ProcessUnprocessedLogsAsync(stoppingToken);

                // Then run on interval
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await ProcessUnprocessedLogsAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Job creation operation cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during job creation cycle");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job Creation Service stopping gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Job Creation Service encountered a fatal error");
                throw;
            }

            _logger.LogInformation("Job Creation Service stopped");
        }

        private async Task ProcessUnprocessedLogsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();

            var unprocessedLogs = await jobRepository.GetUnprocessedFileChangeLogsAsync(BatchSize);

            if (unprocessedLogs.Count == 0)
            {
                _logger.LogDebug("No unprocessed file change logs found");
                return;
            }

            _logger.LogInformation("Processing {Count} unprocessed file change logs", unprocessedLogs.Count);

            var successCount = 0;
            var errorCount = 0;

            foreach (var log in unprocessedLogs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var job = await jobService.CreateJobFromLogAsync(log);
                    successCount++;

                    _logger.LogInformation(
                        "Created job {JobId} from log {LogId} for file {FileId}",
                        job.Id,
                        log.Id,
                        log.FileId);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(
                        ex,
                        "Failed to create job from log {LogId} for file {FileId}",
                        log.Id,
                        log.FileId);
                }
            }

            _logger.LogInformation(
                "Job creation cycle completed. Success: {SuccessCount}, Errors: {ErrorCount}",
                successCount,
                errorCount);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Job Creation Service stopping...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Job Creation Service stopped gracefully");
        }
    }
}
