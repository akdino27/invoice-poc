using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize]
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadInvoice([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No file uploaded" });
            }

            try
            {
                var vendorId = GetVendorIdAsGuid();
                var result = await _vendorInvoiceService.UploadInvoiceAsync(vendorId, file);
                return Ok(result);
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
}
