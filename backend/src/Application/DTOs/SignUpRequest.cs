using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace invoice_v1.src.Application.DTOs
{
    public class SignupRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        public string? DriveFolderId { get; set; }

        // FIX: Custom validation for strong password
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Password))
            {
                var errors = new List<string>();

                // Check for at least one uppercase letter
                if (!Regex.IsMatch(Password, @"[A-Z]"))
                {
                    errors.Add("at least one uppercase letter");
                }

                // Check for at least one lowercase letter
                if (!Regex.IsMatch(Password, @"[a-z]"))
                {
                    errors.Add("at least one lowercase letter");
                }

                // Check for at least one digit
                if (!Regex.IsMatch(Password, @"\d"))
                {
                    errors.Add("at least one digit");
                }

                // Check for at least one special character
                if (!Regex.IsMatch(Password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
                {
                    errors.Add("at least one special character (!@#$%^&*()_+-=[]{}etc.)");
                }

                // Check for common weak passwords
                var weakPasswords = new[] {
                    "password", "12345678", "qwerty12", "abc123!@", "admin123",
                    "welcome1", "password1", "letmein1"
                };

                if (weakPasswords.Any(wp => Password.ToLower().Contains(wp)))
                {
                    errors.Add("password is too common");
                }

                if (errors.Any())
                {
                    yield return new ValidationResult(
                        $"Password must contain {string.Join(", ", errors)}",
                        new[] { nameof(Password) });
                }
            }
        }
    }
}
