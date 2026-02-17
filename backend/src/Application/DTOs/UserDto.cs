namespace invoice_v1.src.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
