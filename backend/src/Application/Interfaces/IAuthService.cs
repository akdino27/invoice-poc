using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IAuthService
    {
        Task SignupAsync(SignupRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);
    }
}
