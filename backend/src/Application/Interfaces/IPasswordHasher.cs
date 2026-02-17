namespace invoice_v1.src.Application.Security
{
    public interface IPasswordHasher
    {
        (string Hash, string Salt) HashPassword(string password);

        bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt);
    }
}
