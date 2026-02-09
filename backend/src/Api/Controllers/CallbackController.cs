using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

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
        public async Task<IActionResult> HandleCallback()
        {
            Request.EnableBuffering();

            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            if (!Request.Headers.TryGetValue("X-Callback-HMAC", out var hmacHeader))
            {
                return Unauthorized(new { error = "Missing X-Callback-HMAC header" });
            }

            if (!_hmacValidator.ValidateHmac(requestBody, hmacHeader!))
            {
                return Unauthorized(new { error = "Invalid HMAC signature" });
            }

            CallbackRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CallbackRequest>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "Invalid JSON format" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Invalid request payload" });
            }

            var job = await _jobService.GetJobByIdAsync(request.JobId);
            if (job == null)
            {
                return NotFound(new { error = $"Job {request.JobId} not found" });
            }

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
                    return BadRequest(new { error = $"Invalid status: {request.Status}" });
            }

            return Ok(new
            {
                success = true,
                jobId = request.JobId,
                status = request.Status
            });
        }

        private async Task HandleCompletedCallbackAsync(CallbackRequest request)
        {
            if (request.Result == null)
                throw new ArgumentException("Result is required for COMPLETED status");

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var invoice = await _invoiceService.CreateOrUpdateInvoiceFromCallbackAsync(
                    request.JobId,
                    request.Result);

                var job = await _context.JobQueues.FindAsync(request.JobId);
                if (job != null)
                {
                    job.Status = "COMPLETED";
                    job.UpdatedAt = DateTime.UtcNow;
                    job.LockedBy = null;
                    job.LockedAt = null;

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Job {JobId} completed. Invoice {InvoiceId} processed",
                    request.JobId,
                    invoice.Id);
            });
        }

        private async Task HandleInvalidCallbackAsync(CallbackRequest request)
        {
            var reasonJson = JsonDocument.Parse(
                JsonSerializer.Serialize(new
                {
                    message = request.Reason ?? "No reason provided"
                })
            );

            await _jobService.MarkInvalidAsync(request.JobId, reasonJson);

            var jobEntity = await _context.JobQueues
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == request.JobId);

            if (jobEntity?.PayloadJson != null)
            {
                var root = jobEntity.PayloadJson.RootElement;


                var fileId = root.TryGetProperty("fileId", out var fid) ? fid.GetString() : null;
                var fileName = root.TryGetProperty("originalName", out var fn) ? fn.GetString() : null;

                var invalidInvoice = new InvalidInvoice
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    FileName = fileName,
                    Reason = reasonJson,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InvalidInvoices.Add(invalidInvoice);
                await _context.SaveChangesAsync();
            }
        }

        private async Task HandleFailedCallbackAsync(CallbackRequest request)
        {
            var errorJson = JsonDocument.Parse(
                JsonSerializer.Serialize(new
                {
                    message = request.Reason ?? "Worker reported failure"
                })
            );

            await _jobService.MarkFailedAsync(request.JobId, errorJson);

            _logger.LogError(
                "Job {JobId} marked as FAILED",
                request.JobId);
        }
    }
}
