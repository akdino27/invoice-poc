using invoice_v1.src.Application.DTOs;
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

        [HttpGet]
        [ProducesResponseType(typeof(JobListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetJobs(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var vendorId = GetVendorIdIfVendor();

            JobStatus? jobStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<JobStatus>(status, true, out var parsedStatus))
                {
                    return BadRequest(new { error = $"Invalid status '{status}'" });
                }
                jobStatus = parsedStatus;
            }

            var (jobs, total) = await _jobService.GetJobsAsync(
                jobStatus,
                page,
                pageSize,
                vendorId);

            var response = new JobListResponse
            {
                Jobs = jobs,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetJobById(Guid id)
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
            {
                return NotFound(new { error = $"Job {id} not found" });
            }

            var vendorId = GetVendorIdIfVendor();
            if (vendorId.HasValue)
            {
                var canAccess = await _jobService.CanVendorAccessJobAsync(id, vendorId.Value);
                if (!canAccess)
                {
                    _logger.LogWarning(
                        "Vendor {VendorId} attempted to access job {JobId} they don't own",
                        vendorId.Value,
                        id);
                    return Forbid();
                }
            }

            return Ok(job);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:guid}/requeue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequeueJob(Guid id)
        {
            try
            {
                await _jobService.RequeueJobAsync(id);

                _logger.LogInformation(
                    "Job {JobId} requeued by admin {AdminId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

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

        private Guid? GetVendorIdIfVendor()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == UserRole.Vendor.ToString())
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdClaim, out var vendorId))
                {
                    _logger.LogDebug("Jobs request from vendor {VendorId}", vendorId);
                    return vendorId;
                }
            }

            return null;
        }
    }
}
