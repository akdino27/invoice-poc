using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Api.Filters;
using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// <summary>
    /// Handles job management operations with RBAC support.
    /// Added RBAC enforcement to prevent vendors from seeing other vendors' jobs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(RbacActionFilter))]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            IJobService jobService,
            IConfiguration configuration,
            ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Lists jobs with optional filtering by status.
        /// Added RBAC - vendors see only their jobs, admins see all.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(JobListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetJobs(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            JobStatus? jobStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<JobStatus>(status, true, out var parsedStatus))
                {
                    return BadRequest(new { error = $"Invalid status: {status}" });
                }
                jobStatus = parsedStatus;
            }

            // RBAC: Get jobs filtered by vendor email for non-admins
            var (jobs, total) = await _jobService.GetJobsAsync(
                jobStatus,
                page,
                pageSize,
                userEmail,
                isAdmin);

            var response = new JobListResponse
            {
                Jobs = jobs,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets a specific job by ID.
        /// FIXED: Added RBAC - vendors can only see their own jobs.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJobById(Guid id)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var job = await _jobService.GetJobByIdAsync(id, userEmail, isAdmin);

            if (job == null)
            {
                return NotFound(new { error = $"Job {id} not found" });
            }

            return Ok(job);
        }

        /// <summary>
        /// Requeues a failed job (admin endpoint).
        /// </summary>
        [HttpPost("{id}/requeue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequeueJob(Guid id)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            // RBAC: Only admins can requeue jobs
            if (!isAdmin)
            {
                _logger.LogWarning("Non-admin user {UserEmail} attempted to requeue job {JobId}", userEmail, id);
                return StatusCode(403, new { error = "Admin access required" });
            }

            try
            {
                await _jobService.RequeueJobAsync(id);

                _logger.LogInformation("Job {JobId} requeued by admin {UserEmail}", id, userEmail);

                return Ok(new
                {
                    success = true,
                    jobId = id,
                    message = "Job requeued successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requeuing job {JobId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
