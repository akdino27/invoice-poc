using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (logs, total) = await logService.GetLogsAsync(page, pageSize);

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
