using Microsoft.AspNetCore.Identity.Data;

namespace invoice_v1.src.Application.Interfaces
{
    public interface IAuthService
    {
        Task SignupAsync(SignupRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);
    }
}
