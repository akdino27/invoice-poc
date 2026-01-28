using invoice_v1.src.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace invoice_v1.src.Application.Services
{
    /// <summary>
    /// Implements HMAC-SHA256 signature validation for secure API callbacks.
    /// Used to verify that callback requests originate from authorized workers.
    /// </summary>
    public class HmacValidator : IHmacValidator
    {
        private readonly string _secret;
        private readonly ILogger<HmacValidator> _logger;

        public HmacValidator(IConfiguration configuration, ILogger<HmacValidator> logger)
        {
            _secret = configuration["Security:CallbackSecret"]
                ?? throw new InvalidOperationException(
                    "Security:CallbackSecret is not configured. Set AI_CALLBACK_SECRET environment variable.");
            _logger = logger;
        }

        public bool ValidateHmac(string requestBody, string providedHmac)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("HMAC validation failed: empty request body");
                return false;
            }

            if (string.IsNullOrWhiteSpace(providedHmac))
            {
                _logger.LogWarning("HMAC validation failed: no HMAC provided");
                return false;
            }

            try
            {
                var computedHmac = ComputeHmac(requestBody);
                var isValid = computedHmac.Equals(providedHmac, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning(
                        "HMAC validation failed. Expected: {ExpectedHmac}, Got: {ProvidedHmac}",
                        computedHmac.Substring(0, Math.Min(8, computedHmac.Length)) + "...",
                        providedHmac.Substring(0, Math.Min(8, providedHmac.Length)) + "...");
                }
                else
                {
                    _logger.LogDebug("HMAC validation successful");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HMAC validation");
                return false;
            }
        }

        public string ComputeHmac(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
