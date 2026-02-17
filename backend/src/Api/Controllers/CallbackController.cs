using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<CallbackController> _logger;

        public CallbackController(
            IJobService jobService,
            IInvoiceService invoiceService,
            IHmacValidator hmacValidator,
            ILogger<CallbackController> logger)
        {
            _jobService = jobService;
            _invoiceService = invoiceService;
            _hmacValidator = hmacValidator;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> HandleCallback()
        {
            string requestBody;

            try
            {
                Request.EnableBuffering();

                using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                return BadRequest(new { error = "Failed to read request body" });
            }

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
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in callback request");
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

            // FIX: Idempotency check - don't process if job already in final state
            if (job.Status == "Completed" || job.Status == "Invalid")
            {
                _logger.LogWarning(
                    "Callback received for job {JobId} already in final state {Status}, ignoring",
                    request.JobId,
                    job.Status);

                return Ok(new
                {
                    success = true,
                    jobId = request.JobId,
                    status = job.Status,
                    message = "Job already processed (idempotent)"
                });
            }

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing callback for job {JobId} with status {Status}",
                    request.JobId,
                    request.Status);

                return StatusCode(500, new { error = "Error processing callback" });
            }
        }

        private async Task HandleCompletedCallbackAsync(CallbackRequest request)
        {
            if (request.Result == null)
                throw new ArgumentException("Result is required for COMPLETED status");

            var invoice = await _invoiceService.CreateOrUpdateInvoiceFromCallbackAsync(
                request.JobId,
                request.Result);

            await _jobService.CompleteJobAsync(request.JobId);

            _logger.LogInformation(
                "Job {JobId} completed. Invoice {InvoiceId} processed",
                request.JobId,
                invoice.Id);
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

            await _jobService.CreateInvalidInvoiceFromJobAsync(request.JobId, reasonJson);
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
