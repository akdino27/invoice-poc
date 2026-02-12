using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        // =====================================================
        // GET: /api/admin/users/pending
        // =====================================================
        [HttpGet("users/pending")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var users = await _adminUserService.GetPendingUsersAsync();
            return Ok(users);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminUserService.GetAllUsersAsync();
            return Ok(users);
        }

        // =====================================================
        // POST: /api/admin/users/{id}/approve
        // =====================================================
        [HttpPost("users/{id:guid}/approve")]
        public async Task<IActionResult> ApproveUser(Guid id)
        {
            var adminId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _adminUserService.ApproveUserAsync(id, adminId);

            return Ok(new { message = "User approved successfully" });
        }

        // =====================================================
        // POST: /api/admin/users/{id}/reject
        // =====================================================
        [HttpPost("users/{id:guid}/reject")]
        public async Task<IActionResult> RejectUser(
            Guid id,
            [FromBody] RejectUserRequest request)
        {
            var adminId = Guid.Empty;

            await _adminUserService.RejectUserAsync(
                id,
                adminId,
                request.Reason);

            return Ok(new { message = "User rejected successfully" });
        }

        // POST: /api/admin/users/{id}/promote
        [HttpPost("users/{id:guid}/promote")]
        public async Task<IActionResult> PromoteUser(Guid id)
        {
            var adminId = Guid.Empty; // JWT later

            await _adminUserService.PromoteToAdminAsync(id, adminId);

            return Ok(new { message = "User promoted to admin successfully" });
        }

        // DELETE: /api/admin/users/{id}
        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var adminId = Guid.Empty;

            await _adminUserService.SoftDeleteUserAsync(id, adminId);

            return Ok(new { message = "User deleted successfully" });
        }


    }

    public class RejectUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
