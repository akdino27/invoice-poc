using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    /// <summary>
    /// Vendor management endpoints with RBAC support.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(RbacActionFilter))]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService vendorService;
        private readonly ILogger<VendorsController> logger;

        public VendorsController(
            IVendorService vendorService,
            ILogger<VendorsController> logger)
        {
            this.vendorService = vendorService;
            this.logger = logger;
        }

        /// <summary>
        /// Get current vendor info (self).
        /// Moved before {email} route to prevent route conflict.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyVendorInfo()
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            var vendor = await vendorService.GetVendorByEmailAsync(userEmail);

            if (vendor == null)
            {
                // Auto-create vendor if doesn't exist
                await vendorService.GetOrCreateVendorAsync(userEmail);
                vendor = await vendorService.GetVendorByEmailAsync(userEmail);
            }

            return Ok(vendor);
        }

        /// <summary>
        /// Get all vendors (admin-only).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(VendorListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetVendors(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            if (!isAdmin)
            {
                logger.LogWarning("Non-admin user {UserEmail} attempted to access vendor list", userEmail);
                return StatusCode(403, new { error = "Admin access required" });
            }

            var skip = (page - 1) * pageSize;
            var vendors = await vendorService.GetAllVendorsAsync(skip, pageSize);
            var total = await vendorService.GetVendorCountAsync();

            var response = new VendorListResponse
            {
                Vendors = vendors,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return Ok(response);
        }

        /// <summary>
        /// Get vendor by email.
        /// Vendors can access their own info, admins can access any vendor.
        /// </summary>
        [HttpGet("{email}")]
        [ProducesResponseType(typeof(VendorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVendorByEmail(string email)
        {
            var (userEmail, isAdmin) = this.GetUserContext();

            // RBAC: Vendors can only access their own info
            if (!isAdmin && !email.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("User {UserEmail} attempted to access vendor {Email}", userEmail, email);
                return StatusCode(403, new { error = "Access denied" });
            }

            var vendor = await vendorService.GetVendorByEmailAsync(email);

            if (vendor == null)
            {
                return NotFound(new { error = $"Vendor {email} not found" });
            }

            return Ok(vendor);
        }
    }
}
