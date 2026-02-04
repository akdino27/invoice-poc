using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Services
{
    public class CallbackService : ICallbackService
    {
        private readonly IJobService jobService;
        private readonly IInvoiceService invoiceService;
        private readonly IInvalidInvoiceService invalidInvoiceService;
        private readonly IJobRepository jobRepository;
        private readonly ApplicationDbContext context;
        private readonly ILogger<CallbackService> logger;
        private const int MaxRetries = 3;

        public CallbackService(
            IJobService jobService,
            IInvoiceService invoiceService,
            IInvalidInvoiceService invalidInvoiceService,
            IJobRepository jobRepository,
            ApplicationDbContext context,
            ILogger<CallbackService> logger)
        {
            this.jobService = jobService;
            this.invoiceService = invoiceService;
            this.invalidInvoiceService = invalidInvoiceService;
            this.jobRepository = jobRepository;
            this.context = context;
            this.logger = logger;
        }

        public async Task<CallbackResult> ProcessCallbackAsync(CallbackRequest request)
        {
            var job = await jobService.GetJobByIdAsync(request.JobId);
            if (job == null)
            {
                logger.LogWarning("Callback rejected: Job {JobId} not found", request.JobId);
                throw new ArgumentException($"Job {request.JobId} not found");
            }

            logger.LogInformation("Processing callback for job {JobId} with status {Status}",
                request.JobId, request.Status);

            return request.Status.ToUpperInvariant() switch
            {
                "COMPLETED" => await HandleCompletedAsync(request),
                "INVALID" => await HandleInvalidAsync(request),
                "FAILED" => await HandleFailedAsync(request),
                _ => throw new ArgumentException($"Invalid status: {request.Status}")
            };
        }

        private async Task<CallbackResult> HandleCompletedAsync(CallbackRequest request)
        {
            if (request.Result == null)
                throw new ArgumentException("Result is required for COMPLETED status");

            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var invoice = await invoiceService.CreateOrUpdateInvoiceFromCallbackAsync(
                        request.JobId,
                        request.Result);

                    var job = await jobRepository.GetByIdAsync(request.JobId);
                    if (job != null)
                    {
                        job.Status = "COMPLETED";
                        job.UpdatedAt = DateTime.UtcNow;
                        job.LockedBy = null;
                        job.LockedAt = null;
                        await jobRepository.UpdateJobAsync(job);
                    }

                    await transaction.CommitAsync();

                    logger.LogInformation("Job {JobId} completed successfully. Invoice {InvoiceId} created/updated",
                        request.JobId, invoice.Id);

                    return new CallbackResult
                    {
                        Success = true,
                        Message = "Invoice processed successfully",
                        Data = invoice
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Error in transaction for job {JobId}", request.JobId);
                    throw;
                }
            });
        }

        private async Task<CallbackResult> HandleInvalidAsync(CallbackRequest request)
        {
            var reason = request.Reason ?? "No reason provided";

            await jobService.MarkInvalidAsync(request.JobId, reason);
            await invalidInvoiceService.CreateInvalidInvoiceFromJobAsync(request.JobId, reason);

            logger.LogWarning("Job {JobId} marked as INVALID. Reason: {Reason}",
                request.JobId, reason);

            return new CallbackResult
            {
                Success = true,
                Message = "Marked as invalid invoice"
            };
        }

        private async Task<CallbackResult> HandleFailedAsync(CallbackRequest request)
        {
            logger.LogInformation("HandleFailedCallbackAsync called for job {JobId}", request.JobId);

            var errorMessage = request.Reason ?? "Worker reported failure";

            await jobService.MarkFailedAsync(request.JobId, errorMessage);

            var job = await jobService.GetJobByIdAsync(request.JobId);

            if (job != null)
            {
                logger.LogInformation("Job {JobId} has RetryCount: {RetryCount}, MaxRetries: {MaxRetries}",
                    request.JobId, job.RetryCount, MaxRetries);

                if (job.RetryCount >= MaxRetries)
                {
                    logger.LogInformation("Creating InvalidInvoice entry for job {JobId}", request.JobId);

                    await invalidInvoiceService.CreateInvalidInvoiceFromJobAsync(
                        request.JobId,
                        $"[FAILED_AFTER_{MaxRetries}_ATTEMPTS] {errorMessage}");

                    logger.LogError("Job {JobId} permanently FAILED after {RetryCount} attempts",
                        request.JobId, job.RetryCount);

                    return new CallbackResult
                    {
                        Success = true,
                        Message = "Job permanently failed"
                    };
                }

                logger.LogWarning("Job {JobId} marked as FAILED (attempt {RetryCount}/{MaxRetries}). Will retry.",
                    request.JobId, job.RetryCount, MaxRetries);
            }

            return new CallbackResult
            {
                Success = true,
                Message = "Job failed, will retry"
            };
        }
    }
}
