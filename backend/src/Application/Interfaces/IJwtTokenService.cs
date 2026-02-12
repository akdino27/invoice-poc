using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Application.Security
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);
    }
}
