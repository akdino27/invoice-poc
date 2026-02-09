using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// <summary>
    /// File change log endpoints with RBAC support.
    /// FIXED: Added RBAC enforcement to prevent vendors from seeing other vendors' logs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(RbacActionFilter))]
    public class LogsController : ControllerBase
    {
        private readonly ILogService logService;
        private readonly ILogger<LogsController> logger;

        public LogsController(
            ILogService logService,
            ILogger<LogsController> logger)
        {
            this.logService = logService;
            this.logger = logger;
        }

        /// <summary>
        /// Get file change logs with pagination.
        /// Added RBAC - vendors see only their logs, admins see all.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            // Filter logs by vendor email for non-admins
            var (logs, total) = await logService.GetLogsAsync(page, pageSize, userEmail, isAdmin);

            return Ok(new
            {
                logs,
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
    }
}
