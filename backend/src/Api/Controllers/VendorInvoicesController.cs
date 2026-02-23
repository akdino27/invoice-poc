using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Exceptions;
using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize(Roles = "Vendor")]
    [ApiController]
    [Route("api/[controller]")]
    public class VendorInvoicesController : ControllerBase
    {
        private readonly IVendorInvoiceService _vendorInvoiceService;
        private readonly ILogger<VendorInvoicesController> _logger;

        public VendorInvoicesController(
            IVendorInvoiceService vendorInvoiceService,
            ILogger<VendorInvoicesController> logger)
        {
            _vendorInvoiceService = vendorInvoiceService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> UploadInvoice([FromForm] UploadInvoiceRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { Message = "No file uploaded" });
            }

            try
            {
                var vendorId = GetVendorIdAsGuid();
                var result = await _vendorInvoiceService.UploadInvoiceAsync(vendorId, request.File);

                if (!result.Success)
                {
                    return UnprocessableEntity(new { result.Message, result.SecurityReason });
                }

                return Ok(result);
            }
            catch (RateLimitExceededException ex)
            {
                _logger.LogWarning(ex, "Rate limit exceeded for upload");
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Upload failed - vendor not found");
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading invoice");
                return StatusCode(500, new { Message = "An error occurred while uploading the file" });
            }
        }

        private Guid GetVendorIdAsGuid()
        {
            var vendorIdString = User.FindFirstValue("VendorId")
                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new UnauthorizedAccessException("Vendor ID not found in token");

            if (!Guid.TryParse(vendorIdString, out var vendorId))
            {
                throw new UnauthorizedAccessException("Invalid Vendor ID format in token");
            }

            return vendorId;
        }
    }

    /// <summary>
    /// Request model for file upload — wraps IFormFile so Swashbuckle can generate a proper schema.
    /// </summary>
    public class UploadInvoiceRequest
    {
        /// <summary>
        /// The invoice file to upload (PDF, JPEG, or PNG).
        /// </summary>
        public IFormFile File { get; set; } = null!;
    }
}
