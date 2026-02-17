namespace invoice_v1.src.Application.DTOs
{
    public class LoginResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!; // Added UserDto here
    }
}
