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

        public JobCreationService(IServiceProvider serviceProvider, ILogger<JobCreationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Creation Service is starting.");

            // Initial delay to let app startup completely
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await DoWorkAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during job creation cycle.");
                }
            }
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fileChangeLogRepository = scope.ServiceProvider.GetRequiredService<IFileChangeLogRepository>();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();

            // 1. Get unprocessed HEALTHY logs only (FileId is already set to real Drive ID)
            var unprocessedLogs = await fileChangeLogRepository.GetUnprocessedHealthyLogsAsync(50);
            if (unprocessedLogs.Count == 0) return;

            _logger.LogInformation("Found {Count} unprocessed healthy file logs.", unprocessedLogs.Count);

            foreach (var log in unprocessedLogs)
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        // 2. Create Job in Database
                        await jobService.CreateJobFromLogAsync(log);

                        // 3. Mark Log as Processed
                        log.Processed = true;
                        log.ProcessedAt = DateTime.UtcNow;
                        await fileChangeLogRepository.UpdateAsync(log);
                        await fileChangeLogRepository.SaveChangesAsync();

                        await transaction.CommitAsync(cancellationToken);
                        _logger.LogInformation("Created job for file {FileId}.", log.FileId);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogError(ex, "Failed to create job for file {FileId}", log.FileId);
                    }
                });
            }
        }
    }
}
