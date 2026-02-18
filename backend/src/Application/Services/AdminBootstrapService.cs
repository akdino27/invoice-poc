using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Domain.Enums;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Application.Security
{
    public class AdminBootstrapService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminBootstrapService> _logger;

        public AdminBootstrapService(
            ApplicationDbContext context,
            IConfiguration config,
            IPasswordHasher passwordHasher,
            ILogger<AdminBootstrapService> logger)
        {
            _context = context;
            _config = config;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task EnsureAdminExistsAsync()
        {
            var adminExists = await _context.Users
                .AnyAsync(u => u.Role == UserRole.Admin && !u.IsSoftDeleted);

            if (adminExists)
            {
                _logger.LogInformation("Admin already exists, skipping bootstrap");
                return;
            }

            var email = _config["AdminBootstrap:Email"];
            var password = _config["AdminBootstrap:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning(
                    "AdminBootstrap settings missing, cannot create admin");
                return;
            }

            var (hash, salt) = _passwordHasher.HashPassword(password);

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = email.Trim().ToLowerInvariant(),
                Username = email.Trim().ToLowerInvariant().Split('@')[0],
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Admin,
                Status = UserStatus.Approved,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "FIRST ADMIN ACCOUNT CREATED: {Email}",
                admin.Email);
        }
    }
}
