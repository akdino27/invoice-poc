using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.BackgroundServices
{
    public class JobCreationService : BackgroundService
    {
        private readonly ILogger<JobCreationService> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan interval;

        public JobCreationService(
            ILogger<JobCreationService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;

            // Reduced from 30s to 5s for faster response
            var intervalSeconds = configuration.GetValue<int>("JobCreationIntervalSeconds", 5);
            this.interval = TimeSpan.FromSeconds(intervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Job Creation Service starting. Interval: {Interval}", interval);

            try
            {
                using var timer = new PeriodicTimer(interval);

                // Run immediately on startup
                await DoWork(stoppingToken);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await DoWork(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("Job creation operation was cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during job creation cycle");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Job Creation Service stopping gracefully");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Job Creation Service encountered a fatal error");
                throw;
            }

            logger.LogInformation("Job Creation Service stopped");
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var fileChangeLogRepository = scope.ServiceProvider
                    .GetRequiredService<IFileChangeLogRepository>();
                var jobService = scope.ServiceProvider
                    .GetRequiredService<IJobService>();

                var logs = await fileChangeLogRepository.GetUnprocessedAsync(batchSize: 50);

                if (logs.Count == 0)
                {
                    logger.LogDebug("No unprocessed file change logs");
                    return;
                }

                logger.LogInformation("Processing {Count} unprocessed file change logs", logs.Count);

                int successCount = 0;
                int errorCount = 0;

                foreach (var log in logs)
                {
                    try
                    {
                        var job = await jobService.CreateJobFromLogAsync(log);

                        // Mark log as processed to prevent duplicate jobs
                        await fileChangeLogRepository.MarkAsProcessedAsync(log.Id);
                        successCount++;

                        logger.LogInformation(
                            "Created job {JobId} from log {LogId} for file {FileId}",
                            job.Id, log.Id, log.FileId);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        logger.LogError(ex,
                            "Error creating job from log {LogId} (file: {FileId})",
                            log.Id, log.FileId);
                    }
                }

                logger.LogInformation(
                    "Job creation cycle completed. Success: {SuccessCount}, Errors: {ErrorCount}",
                    successCount, errorCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in job creation cycle");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Job Creation Service stopping...");
            await base.StopAsync(cancellationToken);
            logger.LogInformation("Job Creation Service stopped gracefully");
        }
    }
}
