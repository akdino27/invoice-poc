using System.Security.Cryptography;
using System.Text;

namespace invoice_v1.src.Application.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 32;          // 256 bits
        private const int HashSize = 32;          // 256 bits
        private const int Iterations = 100_000;   // OWASP recommended baseline

        public (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: saltBytes,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: HashSize
            );

            return (
                Hash: Convert.ToBase64String(hashBytes),
                Salt: Convert.ToBase64String(saltBytes)
            );
        }

        public bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var expectedHash = Convert.FromBase64String(storedHash);

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: saltBytes,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: HashSize
            );

            // Constant-time comparison
            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash
            );
        }
    }
}
