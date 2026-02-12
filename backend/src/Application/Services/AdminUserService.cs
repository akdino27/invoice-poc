using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminUserService> _logger;

        public AdminUserService(
            ApplicationDbContext context,
            ILogger<AdminUserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================
        // GET PENDING USERS
        // ================================
        public async Task<List<User>> GetPendingUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Role == UserRole.Vendor &&
                    u.Status == UserStatus.Pending &&
                    !u.IsSoftDeleted)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u =>
                    !u.IsSoftDeleted)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        // ================================
        // APPROVE USER
        // ================================
        public async Task ApproveUserAsync(Guid userId, Guid adminId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsSoftDeleted);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (user.Role != UserRole.Vendor)
                throw new InvalidOperationException("Only vendors require approval");

            if (user.Status != UserStatus.Pending)
                throw new InvalidOperationException("User is not pending approval");

            user.Status = UserStatus.Approved;
            user.ApprovedByAdminId = adminId;
            user.ApprovedAt = DateTime.UtcNow;
            user.RejectionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} approved by admin {AdminId}",
                userId,
                adminId);
        }

        // ================================
        // REJECT USER
        // ================================
        public async Task RejectUserAsync(Guid userId, Guid adminId, string reason)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsSoftDeleted);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (user.Role != UserRole.Vendor)
                throw new InvalidOperationException("Only vendors require approval");

            if (user.Status != UserStatus.Pending)
                throw new InvalidOperationException("User is not pending approval");

            user.Status = UserStatus.Rejected;
            user.ApprovedByAdminId = adminId;
            user.ApprovedAt = DateTime.UtcNow;
            user.RejectionReason = reason;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "User {UserId} rejected by admin {AdminId}. Reason: {Reason}",
                userId,
                adminId,
                reason);
        }

        public async Task SoftDeleteUserAsync(Guid userId, Guid adminId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsSoftDeleted);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (user.Id == adminId)
                throw new InvalidOperationException("Admin cannot delete themselves");

            user.IsSoftDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "User {UserId} soft-deleted by admin {AdminId}",
                userId,
                adminId);
        }

        public async Task PromoteToAdminAsync(Guid userId, Guid adminId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsSoftDeleted);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (user.Role == UserRole.Admin)
                throw new InvalidOperationException("User is already an admin");

            user.Role = UserRole.Admin;
            user.Status = UserStatus.Approved;
            user.ApprovedByAdminId = adminId;
            user.ApprovedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} promoted to ADMIN by {AdminId}",
                userId,
                adminId);
        }

    }
}
