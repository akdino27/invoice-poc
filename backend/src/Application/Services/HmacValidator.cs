using invoice_v1.src.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace invoice_v1.src.Application.Services
{
    public class HmacValidator : IHmacValidator
    {
        private readonly string _secret;
        private readonly ILogger<HmacValidator> _logger;

        public HmacValidator(IConfiguration configuration, ILogger<HmacValidator> logger)
        {
            _logger = logger;

            _secret = configuration["Security:CallbackSecret"]
                ?? throw new InvalidOperationException(
                    "Security:CallbackSecret is not configured. Please set this in appsettings.json or environment variables.");

            // FIX: Validate secret strength
            ValidateSecretStrength(_secret);
        }

        public bool ValidateHmac(string payload, string receivedHmac)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogWarning("HMAC validation attempted with empty payload");
                return false;
            }

            if (string.IsNullOrWhiteSpace(receivedHmac))
            {
                _logger.LogWarning("HMAC validation attempted with empty HMAC");
                return false;
            }

            try
            {
                var computedHmac = ComputeHmac(payload);

                // FIX: Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHmac),
                    Encoding.UTF8.GetBytes(receivedHmac));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HMAC validation");
                return false;
            }
        }

        public string ComputeHmac(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentException("Payload cannot be null or empty", nameof(payload));
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        // FIX: Validate secret strength at startup
        private void ValidateSecretStrength(string secret)
        {
            const int MinSecretLength = 32;

            if (secret.Length < MinSecretLength)
            {
                throw new InvalidOperationException(
                    $"Security:CallbackSecret must be at least {MinSecretLength} characters long. " +
                    $"Current length: {secret.Length}");
            }

            // Check for weak secrets
            var weakSecrets = new[]
            {
                "your-secret-key-here",
                "change-me",
                "secret",
                "password",
                "12345678901234567890123456789012"
            };

            if (weakSecrets.Any(ws => secret.Equals(ws, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Security:CallbackSecret is set to a weak/default value. " +
                    "Please use a strong, randomly generated secret.");
            }

            // Check for sufficient entropy (at least some variety in characters)
            var uniqueChars = secret.Distinct().Count();
            if (uniqueChars < 16)
            {
                _logger.LogWarning(
                    "Security:CallbackSecret has low entropy ({UniqueChars} unique characters). " +
                    "Consider using a more complex secret.",
                    uniqueChars);
            }

            _logger.LogInformation(
                "HMAC secret validated successfully (length: {Length}, unique chars: {UniqueChars})",
                secret.Length,
                uniqueChars);
        }
    }
}
