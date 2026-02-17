using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace invoice_v1.src.Api.Controllers
{
    public abstract class BaseAuthenticatedController : ControllerBase
    {
        protected Guid? GetVendorIdIfVendor()
        {
            if (IsAdmin) return null; // Admins see all data
            if (IsVendor) return GetCurrentUserId(); // Vendors see only their data

            // Default safe fallback
            return GetCurrentUserId();
        }

        protected Guid GetCurrentUserId()
        {
            // 1. Try standard NameIdentifier
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Fallback to 'sub' (JWT standard)
            if (string.IsNullOrEmpty(claim)) claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            // 3. Fallback to 'id' (custom)
            if (string.IsNullOrEmpty(claim)) claim = User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID claim is missing or invalid in token.");
            }

            return userId;
        }

        protected bool IsAdmin
        {
            get
            {
                var role = GetUserRole();
                return string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        protected bool IsVendor
        {
            get
            {
                var role = GetUserRole();
                return string.Equals(role, UserRole.Vendor.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private string? GetUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? User.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        }
    }
}
