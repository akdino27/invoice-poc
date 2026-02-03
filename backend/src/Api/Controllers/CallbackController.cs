using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace invoice_v1.src.Api.Controllers
{
    [ApiController]
    [Route("api/ai/[controller]")]
    public class CallbackController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IInvoiceService _invoiceService;
        private readonly IHmacValidator _hmacValidator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallbackController> _logger;

        public CallbackController(
            IJobService jobService,
            IInvoiceService invoiceService,
            IHmacValidator hmacValidator,
            ApplicationDbContext context,
            ILogger<CallbackController> logger)
        {
            _jobService = jobService;
            _invoiceService = invoiceService;
            _hmacValidator = hmacValidator;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> HandleCallback()
        {
            // Enable buffering to allow multiple reads
            Request.EnableBuffering();

            // Read raw request body for HMAC validation
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();

            // Reset stream position for potential re-reads
            Request.Body.Position = 0;

            // Validate HMAC signature
            if (!Request.Headers.TryGetValue("X-Callback-HMAC", out var hmacHeader))
            {
                _logger.LogWarning("Callback rejected: Missing X-Callback-HMAC header");
                return Unauthorized(new { error = "Missing X-Callback-HMAC header" });
            }

            if (!_hmacValidator.ValidateHmac(requestBody, hmacHeader!))
            {
                _logger.LogWarning("Callback rejected: Invalid HMAC signature");
                return Unauthorized(new { error = "Invalid HMAC signature" });
            }

            // Deserialize request after HMAC validation
            CallbackRequest? request;
            try
            {
                request = System.Text.Json.JsonSerializer.Deserialize<CallbackRequest>(
                    requestBody,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (request == null)
                {
                    _logger.LogWarning("Callback rejected: Failed to deserialize request body");
                    return BadRequest(new { error = "Invalid request format" });
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Callback rejected: Invalid JSON");
                return BadRequest(new { error = "Invalid JSON format" });
            }

            _logger.LogInformation(
                "Received callback for job {JobId} with status {Status}",
                request.JobId,
                request.Status);

            try
            {
                // Verify job exists
                var job = await _jobService.GetJobByIdAsync(request.JobId);
                if (job == null)
                {
                    _logger.LogWarning("Callback rejected: Job {JobId} not found", request.JobId);
                    return NotFound(new { error = $"Job {request.JobId} not found" });
                }

                // Process based on status
                switch (request.Status.ToUpperInvariant())
                {
                    case "COMPLETED":
                        await HandleCompletedCallbackAsync(request);
                        break;

                    case "INVALID":
                        await HandleInvalidCallbackAsync(request);
                        break;

                    case "FAILED":
                        await HandleFailedCallbackAsync(request);
                        break;

                    default:
                        _logger.LogWarning(
                            "Callback rejected: Invalid status {Status} for job {JobId}",
                            request.Status,
                            request.JobId);
                        return BadRequest(new { error = $"Invalid status: {request.Status}" });
                }

                return Ok(new
                {
                    success = true,
                    jobId = request.JobId,
                    status = request.Status,
                    message = "Callback processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing callback for job {JobId}", request.JobId);
                return StatusCode(500, new { error = "Internal server error processing callback" });
            }
        }

        private async Task HandleCompletedCallbackAsync(CallbackRequest request)
        {
            if (request.Result == null)
            {
                throw new ArgumentException("Result is required for COMPLETED status");
            }

            // retry support with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create or update invoice from result
                    var invoice = await _invoiceService.CreateOrUpdateInvoiceFromCallbackAsync(
                        request.JobId,
                        request.Result);

                    // Update job status and release lock atomically
                    var job = await _context.JobQueues.FindAsync(request.JobId);
                    if (job != null)
                    {
                        job.Status = "COMPLETED";
                        job.UpdatedAt = DateTime.UtcNow;
                        job.LockedBy = null;  // Release lock
                        job.LockedAt = null;

                        _context.Entry(job).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Job {JobId} completed successfully. Invoice {InvoiceId} created/updated",
                        request.JobId,
                        invoice.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in transaction for job {JobId}", request.JobId);
                    throw;
                }
            });
        }

        private async Task HandleInvalidCallbackAsync(CallbackRequest request)
        {
            var reason = request.Reason ?? "No reason provided";

            // Mark job as invalid
            await _jobService.MarkInvalidAsync(request.JobId, reason);

            // Get file information from job payload
            var job = await _jobService.GetJobByIdAsync(request.JobId);
            if (job?.Payload != null)
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(job.Payload);
                var payloadObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payload);

                var fileId = payloadObj.TryGetProperty("fileId", out var fid) ? fid.GetString() : null;
                var fileName = payloadObj.TryGetProperty("originalName", out var fn) ? fn.GetString() : null;

                // Create InvalidInvoice record
                var invalidInvoice = new InvalidInvoice
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    FileName = fileName,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InvalidInvoices.Add(invalidInvoice);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Job {JobId} marked as INVALID. File {FileId} added to invalid invoices. Reason: {Reason}",
                    request.JobId,
                    fileId,
                    reason);
            }
        }

        private async Task HandleFailedCallbackAsync(CallbackRequest request)
        {
            var errorMessage = request.Reason ?? "Worker reported failure";

            await _jobService.MarkFailedAsync(request.JobId, errorMessage);

            _logger.LogError(
                "Job {JobId} marked as FAILED. Error: {Error}",
                request.JobId,
                errorMessage);
        }
    }
}
