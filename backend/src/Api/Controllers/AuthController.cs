using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<AuthController> _logger;

        // Configuration constants
        private const int MaxLoginAttempts = 5;
        private const int MaxSignupAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan SignupRateLimitWindow = TimeSpan.FromHours(1);

        public AuthController(
            IAuthService authService,
            IRateLimitService rateLimitService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new vendor account
        /// </summary>
        [HttpPost("signup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            try
            {
                // Server-side validation
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.CompanyName)) // Validate Company Name too
                {
                    return BadRequest(new { Message = "Email, Password, and Company Name are required" });
                }

                // Rate limiting check
                var ipAddress = GetClientIpAddress();
                var signupKey = $"signup_{ipAddress}";

                if (await _rateLimitService.IsRateLimitedAsync(
                    signupKey,
                    MaxSignupAttempts,
                    SignupRateLimitWindow))
                {
                    var attempts = await _rateLimitService.GetAttemptsAsync(signupKey);
                    _logger.LogWarning(
                        "Too many signup attempts from IP {IpAddress}: {Attempts}/{Max}",
                        ipAddress,
                        attempts,
                        MaxSignupAttempts);

                    return StatusCode(429, new
                    {
                        Message = "Too many signup attempts. Please try again later."
                    });
                }

                await _authService.SignupAsync(request);

                // Increment signup attempt counter
                await _rateLimitService.IncrementAsync(signupKey, SignupRateLimitWindow);

                return Ok(new
                {
                    Message = "Signup successful. Your account is pending approval.",
                    Email = request.Email
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Signup failed for {Email}", request.Email);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup for {Email}", request.Email);
                return StatusCode(500, new { Message = "An error occurred during signup" });
            }
        }

        /// <summary>
        /// Login to existing vendor account
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)] // Updated return type
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { Message = "Email and password are required" });
                }

                // Account Lockout Check
                var loginKey = $"login_{request.Email.ToLowerInvariant()}";

                if (await _rateLimitService.IsRateLimitedAsync(
                    loginKey,
                    MaxLoginAttempts,
                    LockoutDuration))
                {
                    var attempts = await _rateLimitService.GetAttemptsAsync(loginKey);
                    var remainingMinutes = Math.Ceiling(LockoutDuration.TotalMinutes);

                    _logger.LogWarning(
                        "Account {Email} is locked due to too many failed login attempts ({Attempts}/{Max})",
                        request.Email,
                        attempts,
                        MaxLoginAttempts);

                    return StatusCode(429, new
                    {
                        Message = $"Account temporarily locked due to too many failed attempts. Try again in {remainingMinutes} minutes."
                    });
                }

                // Prevent timing attacks
                await Task.Delay(Random.Shared.Next(100, 300));

                var result = await _authService.LoginAsync(request);

                // Reset failed attempts on success
                await _rateLimitService.ResetAsync(loginKey);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Track failed login attempts
                var loginKey = $"login_{request.Email.ToLowerInvariant()}";
                await _rateLimitService.IncrementAsync(loginKey, LockoutDuration);

                var attempts = await _rateLimitService.GetAttemptsAsync(loginKey);

                _logger.LogWarning(
                    ex,
                    "Login failed for {Email} (Attempt {Attempts}/{Max})",
                    request.Email,
                    attempts,
                    MaxLoginAttempts);

                return Unauthorized(new { Message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new { Message = "An error occurred during login" });
            }
        }

        private string GetClientIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrWhiteSpace(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
