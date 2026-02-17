using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize(Roles = "Admin,Vendor")]
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly IFileChangeLogService _fileChangeLogService; 
        private readonly ILogger<LogsController> _logger;

        public LogsController(
            IFileChangeLogService fileChangeLogService, 
            ILogger<LogsController> logger)
        {
            _fileChangeLogService = fileChangeLogService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? changeType = null)
        {
            var vendorId = GetVendorIdIfVendor();

            var result = await _fileChangeLogService.GetLogsAsync(vendorId, changeType, page, pageSize);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLogById(int id)
        {
            var vendorId = GetVendorIdIfVendor();

            var log = await _fileChangeLogService.GetLogByIdAsync(id, vendorId);

            if (log == null)
            {
                return NotFound(new { error = $"Log {id} not found" });
            }

            return Ok(log);
        }

        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogStats()
        {
            var vendorId = GetVendorIdIfVendor();

            // CHANGED: Service call
            var stats = await _fileChangeLogService.GetLogStatsAsync(vendorId);

            return Ok(stats);
        }

        private Guid? GetVendorIdIfVendor()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == UserRole.Vendor.ToString())
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdClaim, out var vendorId))
                {
                    return vendorId;
                }
            }

            return null;
        }
    }
}
