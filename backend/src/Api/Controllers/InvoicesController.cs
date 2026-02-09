using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// <summary>
    /// Handles invoice query operations with RBAC enforcement.
    /// REFACTORED: Uses RbacActionFilter to eliminate duplicate code.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(RbacActionFilter))]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService invoiceService;
        private readonly ILogger<InvoicesController> logger;

        public InvoicesController(
            IInvoiceService invoiceService,
            ILogger<InvoicesController> logger)
        {
            this.invoiceService = invoiceService;
            this.logger = logger;
        }

        /// <summary>
        /// Gets an invoice by ID with RBAC enforcement.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceById(Guid id)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            try
            {
                var invoice = await invoiceService.GetInvoiceByIdAsync(id, userEmail, isAdmin);

                if (invoice == null)
                {
                    return NotFound(new { error = $"Invoice {id} not found" });
                }

                return Ok(invoice);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized access attempt by {UserEmail} for invoice {InvoiceId}", userEmail, id);
                return StatusCode(403, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets an invoice by Drive file ID with RBAC enforcement.
        /// </summary>
        [HttpGet("by-file/{fileId}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceByFileId(string fileId)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            try
            {
                var invoice = await invoiceService.GetInvoiceByFileIdAsync(fileId, userEmail, isAdmin);

                if (invoice == null)
                {
                    return NotFound(new { error = $"No invoice found for file {fileId}" });
                }

                return Ok(invoice);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized access attempt by {UserEmail} for file {FileId}", userEmail, fileId);
                return StatusCode(403, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all invoices for the authenticated vendor.
        /// Admins can optionally filter by vendorEmail query parameter.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetInvoices(
            [FromQuery] string? vendorEmail = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            // Non-admins can only query their own invoices
            var targetVendorEmail = isAdmin ? vendorEmail : userEmail;

            var invoices = await invoiceService.GetInvoicesByVendorAsync(
                targetVendorEmail,
                (page - 1) * pageSize,
                pageSize,
                isAdmin);

            var total = await invoiceService.GetInvoiceCountByVendorAsync(targetVendorEmail, isAdmin);

            return Ok(new
            {
                invoices,
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }
    }
}
