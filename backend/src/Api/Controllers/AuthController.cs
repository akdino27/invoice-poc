using invoice_v1.src.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invoice_v1.src.Api.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user. Vendor accounts require admin approval.
        /// </summary>
        [HttpPost("signup")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.SignupAsync(request);

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Signup successful. Awaiting admin approval."
            });
        }

        /// <summary>
        /// Authenticates a user and returns a JWT access token.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
    }
}
