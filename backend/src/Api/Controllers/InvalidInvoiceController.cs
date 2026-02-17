using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/invalid-invoices")]
    public class InvalidInvoiceController : ControllerBase
    {
        private readonly IInvalidInvoiceRepository _repository;
        private readonly IJobService _jobService;

        public InvalidInvoiceController(
            IInvalidInvoiceRepository repository,
            IJobService jobService)
        {
            _repository = repository;
            _jobService = jobService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            Guid? vendorId = null;

            // Security: If User is Vendor, force filter by their ID
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == UserRole.Vendor.ToString())
            {
                if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var vid))
                {
                    vendorId = vid;
                }
            }

            var (data, total) = await _repository.GetInvalidInvoicesAsync(page, pageSize, vendorId);

            return Ok(new
            {
                data,
                totalCount = total,
                page,
                pageSize
            });
        }

        [HttpPost("{jobId:guid}/requeue")]
        [Authorize(Roles = "Admin")] // Only Admin can requeue
        public async Task<IActionResult> Requeue(Guid jobId)
        {
            try
            {
                await _jobService.RequeueJobAsync(jobId);
                return Ok(new { message = "Job requeued successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
