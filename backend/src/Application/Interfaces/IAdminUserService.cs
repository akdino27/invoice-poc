using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IAdminUserService
    {
        Task<List<User>> GetPendingUsersAsync();
        Task<List<User>> GetAllUsersAsync();
        Task ApproveUserAsync(Guid userId, Guid adminId);
        Task RejectUserAsync(Guid userId, Guid adminId, string reason);
        Task PromoteToAdminAsync(Guid userId, Guid adminId);
        Task SoftDeleteUserAsync(Guid userId, Guid adminId);
        Task UnlockUserAsync(Guid userId, Guid adminId);
    }
}
