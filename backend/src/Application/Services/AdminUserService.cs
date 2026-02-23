using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminUserService> _logger;

        public AdminUserService(
            IUserRepository userRepository,
            ILogger<AdminUserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<User>> GetPendingUsersAsync()
        {
            return await _userRepository.GetPendingVendorsAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task ApproveUserAsync(Guid userId, Guid adminId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            if (user.Status != UserStatus.Pending)
            {
                throw new InvalidOperationException($"User is not in Pending status");
            }

            user.Status = UserStatus.Approved;
            user.ApprovedByAdminId = adminId;
            user.ApprovedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} ({Email}) approved by admin {AdminId}",
                userId,
                user.Email,
                adminId);
        }

        public async Task RejectUserAsync(Guid userId, Guid adminId, string reason)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            if (user.Status != UserStatus.Pending)
            {
                throw new InvalidOperationException($"User is not in Pending status");
            }

            user.Status = UserStatus.Rejected;
            user.RejectionReason = reason;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} ({Email}) rejected by admin {AdminId}. Reason: {Reason}",
                userId,
                user.Email,
                adminId,
                reason);
        }

        public async Task PromoteToAdminAsync(Guid userId, Guid adminId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("User is already an admin");
            }

            user.Role = UserRole.Admin;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} ({Email}) promoted to Admin by {AdminId}",
                userId,
                user.Email,
                adminId);
        }

        public async Task SoftDeleteUserAsync(Guid userId, Guid adminId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            if (user.IsSoftDeleted)
            {
                throw new InvalidOperationException("User is already deleted");
            }

            user.IsSoftDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} ({Email}) soft-deleted by admin {AdminId}",
                userId,
                user.Email,
                adminId);
        }

        public async Task UnlockUserAsync(Guid userId, Guid adminId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            if (user.Status != UserStatus.Locked)
            {
                throw new InvalidOperationException("User is not locked");
            }

            user.Status = UserStatus.Approved;
            user.FailedLoginCount = 0;  // Reset failed login count
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} ({Email}) unlocked by admin {AdminId}",
                userId,
                user.Email,
                adminId);
        }
    }
}
