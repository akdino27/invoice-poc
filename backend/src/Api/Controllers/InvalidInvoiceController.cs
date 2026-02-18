using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/invalid-invoices")]
    public class InvalidInvoiceController : BaseAuthenticatedController
    {
        private readonly IInvalidInvoiceService _invalidInvoiceService;
        private readonly IJobService _jobService;

        public InvalidInvoiceController(
            IInvalidInvoiceService invalidInvoiceService,
            IJobService jobService)
        {
            _invalidInvoiceService = invalidInvoiceService;
            _jobService = jobService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var vendorId = GetVendorIdIfVendor();
            var result = await _invalidInvoiceService.GetInvalidInvoicesAsync(page, pageSize, vendorId);
            return Ok(result);
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
