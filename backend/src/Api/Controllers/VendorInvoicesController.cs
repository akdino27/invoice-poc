using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using invoice_v1.src.Application.Interfaces;
using System.Security.Claims;


namespace invoice_v1.src.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/vendor/invoices")]
public class VendorInvoicesController : ControllerBase
{
    private readonly IVendorInvoiceService _vendorInvoiceService;

    public VendorInvoicesController(IVendorInvoiceService vendorInvoiceService)
    {
        _vendorInvoiceService = vendorInvoiceService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadInvoice(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized("Invalid token");

        var vendorId = Guid.Parse(userIdClaim);


        var result = await _vendorInvoiceService.UploadInvoiceAsync(
            vendorId,
            file);

        return Ok(result);
    }
}
