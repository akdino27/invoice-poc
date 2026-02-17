using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : BaseAuthenticatedController
    {
        private readonly IAdminUserService _adminUserService;

        public AdminController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

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

        [HttpPost("users/{id:guid}/approve")]
        public async Task<IActionResult> ApproveUser(Guid id)
        {
            var adminId = GetCurrentUserId();
            await _adminUserService.ApproveUserAsync(id, adminId);
            return Ok(new { message = "User approved successfully" });
        }

        [HttpPost("users/{id:guid}/reject")]
        public async Task<IActionResult> RejectUser(Guid id, [FromBody] RejectUserRequest request)
        {
            var adminId = GetCurrentUserId();
            await _adminUserService.RejectUserAsync(id, adminId, request.Reason);
            return Ok(new { message = "User rejected successfully" });
        }

        [HttpPost("users/{id:guid}/promote")]
        public async Task<IActionResult> PromoteUser(Guid id)
        {
            var adminId = GetCurrentUserId();
            await _adminUserService.PromoteToAdminAsync(id, adminId);
            return Ok(new { message = "User promoted to admin successfully" });
        }

        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var adminId = GetCurrentUserId();
            await _adminUserService.SoftDeleteUserAsync(id, adminId);
            return Ok(new { message = "User deleted successfully" });
        }

        [HttpPost("users/{id:guid}/unlock")]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var adminId = GetCurrentUserId();
            await _adminUserService.UnlockUserAsync(id, adminId);
            return Ok(new { message = "User unlocked successfully" });
        }
    }
}
