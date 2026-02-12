using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Security;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }


        public async Task SignupAsync(SignupRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var exists = await _context.Users
                .AnyAsync(u => u.Email == email && !u.IsSoftDeleted);

            if (exists)
                throw new InvalidOperationException("Email already registered");

            var (hash, salt) = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Vendor,
                Status = UserStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsSoftDeleted);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid credentials");

            if (!_passwordHasher.VerifyPassword(
                    request.Password,
                    user.PasswordHash,
                    user.PasswordSalt))
            {
                user.FailedLoginCount++;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (user.Status == UserStatus.Pending)
                throw new UnauthorizedAccessException("Account pending admin approval");

            if (user.Status == UserStatus.Rejected)
                throw new UnauthorizedAccessException("Account rejected by admin");

            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginCount = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // JWT will go here next
            var token = _jwtTokenService.GenerateAccessToken(user);

            return new LoginResult
            {
                AccessToken = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

        }

    }
}
