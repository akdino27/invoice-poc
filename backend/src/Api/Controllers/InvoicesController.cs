using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    // Handles invoice query operations.
    // Provides endpoints for retrieving invoice data and extraction results.
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IInvoiceService invoiceService,
            ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        // Gets an invoice by ID.
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceById(Guid id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound(new { error = $"Invoice {id} not found" });
            }

            return Ok(invoice);
        }

        // Gets an invoice by Drive file ID.
        [HttpGet("by-file/{fileId}")]
        [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceByFileId(string fileId)
        {
            var invoice = await _invoiceService.GetInvoiceByFileIdAsync(fileId);
            if (invoice == null)
            {
                return NotFound(new { error = $"No invoice found for file {fileId}" });
            }

            return Ok(invoice);
        }
    }
}
