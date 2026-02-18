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
                      ?? throw new InvalidOperationException("Security:CallbackSecret is not configured.");
        }

        public bool ValidateHmac(string payload, string providedHmac)
        {
            if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(providedHmac))
            {
                return false;
            }

            try
            {
                var computedHmac = ComputeHmac(payload);

                // Use Constant Time Comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHmac),
                    Encoding.UTF8.GetBytes(providedHmac)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HMAC validation.");
                return false;
            }
        }

        public string ComputeHmac(string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}
