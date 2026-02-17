using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.BackgroundServices
{
    public class JobCreationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobCreationService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

        public JobCreationService(
            IServiceProvider serviceProvider,
            ILogger<JobCreationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(" Job Creation Service is starting");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            using var timer = new PeriodicTimer(_interval);

            try
            {
                await DoWorkAsync(stoppingToken);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await DoWorkAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Job creation operation was cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during job creation cycle");
                        // Continue processing on next cycle
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job Creation Service is stopping gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Job Creation Service encountered a fatal error");
                // Don't rethrow - let service continue
            }

            _logger.LogInformation("Job Creation Service has stopped");
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔄 Job Creation check at {Time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var fileChangeLogRepository = scope.ServiceProvider
                    .GetRequiredService<IFileChangeLogRepository>();
                var jobService = scope.ServiceProvider
                    .GetRequiredService<IJobService>();

                // Get unprocessed logs
                var unprocessedLogs = await fileChangeLogRepository.GetUnprocessedLogsAsync(50);

                if (unprocessedLogs.Count == 0)
                {
                    _logger.LogInformation("No unprocessed file logs found");
                    return;
                }

                _logger.LogInformation(
                    "📋 Found {Count} unprocessed file logs, creating jobs...",
                    unprocessedLogs.Count);

                int jobsCreated = 0;
                int jobsFailed = 0;

                foreach (var log in unprocessedLogs)
                {
                    // FIX: Use execution strategy for transaction with retry-on-failure
                    var strategy = dbContext.Database.CreateExecutionStrategy();

                    try
                    {
                        await strategy.ExecuteAsync(async () =>
                        {
                            // Start transaction inside the execution strategy
                            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                            try
                            {
                                await jobService.CreateJobFromLogAsync(log);

                                log.Processed = true;
                                log.ProcessedAt = DateTime.UtcNow;

                                await fileChangeLogRepository.UpdateAsync(log);
                                await fileChangeLogRepository.SaveChangesAsync();

                                await transaction.CommitAsync(cancellationToken);

                                _logger.LogDebug(
                                    " Created job for file {FileId} ({FileName})",
                                    log.FileId,
                                    log.FileName);
                            }
                            catch
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                throw; // Re-throw to be caught by outer catch
                            }
                        });

                        jobsCreated++;
                    }
                    catch (Exception ex)
                    {
                        jobsFailed++;

                        _logger.LogError(
                            ex,
                            "❌ Failed to create job for file {FileId} ({FileName})",
                            log.FileId,
                            log.FileName);

                        // Continue to next log instead of breaking the entire cycle
                    }
                }

                _logger.LogInformation(
                    " Job creation cycle complete. Created: {JobsCreated}, Failed: {JobsFailed}, Total: {LogCount}",
                    jobsCreated,
                    jobsFailed,
                    unprocessedLogs.Count);
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during job creation check");
                // Don't rethrow - log and continue
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(" Job Creation Service is stopping...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation(" Job Creation Service stopped gracefully");
        }
    }
}
