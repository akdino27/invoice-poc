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
        private readonly ICallbackService callbackService;
        private readonly IHmacValidator hmacValidator;
        private readonly ILogger<CallbackController> logger;

        public CallbackController(
            ICallbackService callbackService,
            IHmacValidator hmacValidator,
            ILogger<CallbackController> logger)
        {
            this.callbackService = callbackService;
            this.hmacValidator = hmacValidator;
            this.logger = logger;
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
            Request.EnableBuffering();

            string requestBody;
            using (var reader = new StreamReader(
                Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            logger.LogDebug("Callback request body: {RequestBody}", requestBody);

            var providedHmac = Request.Headers["X-Callback-HMAC"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providedHmac))
            {
                logger.LogWarning("Callback rejected: Missing HMAC signature");
                return Unauthorized(new { error = "Missing HMAC signature" });
            }

            if (!hmacValidator.ValidateHmac(requestBody, providedHmac))
            {
                logger.LogWarning("Callback rejected: Invalid HMAC signature");
                return Unauthorized(new { error = "Invalid HMAC signature" });
            }

            CallbackRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CallbackRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    logger.LogWarning("Failed to deserialize callback request");
                    return BadRequest(new { error = "Invalid request format" });
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Error deserializing callback request");
                return BadRequest(new { error = "Invalid JSON format" });
            }

            try
            {
                var result = await callbackService.ProcessCallbackAsync(request);

                return Ok(new
                {
                    success = result.Success,
                    jobId = request.JobId,
                    status = request.Status,
                    message = result.Message,
                    data = result.Data
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid callback request");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing callback for job {JobId}", request.JobId);
                return StatusCode(500, new { error = "Internal server error processing callback" });
            }
        }
    }
}
