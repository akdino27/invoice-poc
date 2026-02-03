using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// Handles job management operations.
    // Provides endpoints for listing jobs, querying job status, and admin actions.
    [ApiController]
    [Route("api/[controller]")]
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

        // Lists jobs with optional filtering by status.
        [HttpGet]
        [ProducesResponseType(typeof(JobListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetJobs(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            JobStatus? jobStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<JobStatus>(status, true, out var parsedStatus))
                {
                    return BadRequest(new { error = $"Invalid status: {status}" });
                }
                jobStatus = parsedStatus;
            }

            var (jobs, total) = await _jobService.GetJobsAsync(jobStatus, page, pageSize);

            var response = new JobListResponse
            {
                Jobs = jobs,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return Ok(response);
        }

        // Gets a specific job by ID.
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJobById(Guid id)
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
            {
                return NotFound(new { error = $"Job {id} not found" });
            }

            return Ok(job);
        }

        // Requeues a failed job (admin endpoint).
        [HttpPost("{id}/requeue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequeueJob(Guid id)
        {
            // Validate admin API key
            if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            {
                _logger.LogWarning("Requeue rejected: Missing X-Api-Key header");
                return Unauthorized(new { error = "Missing X-Api-Key header" });
            }

            var expectedApiKey = _configuration["Security:AdminApiKey"];
            if (string.IsNullOrWhiteSpace(expectedApiKey))
            {
                _logger.LogError("Admin API key not configured");
                return StatusCode(500, new { error = "Server configuration error" });
            }

            if (apiKey != expectedApiKey)
            {
                _logger.LogWarning("Requeue rejected: Invalid API key");
                return Unauthorized(new { error = "Invalid API key" });
            }

            try
            {
                await _jobService.RequeueJobAsync(id);

                _logger.LogInformation("Job {JobId} requeued by admin", id);

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
