using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models;
using WorkForceGovProject.Services.Common;

namespace WorkForceGovProject.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [Produces("application/json")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthenticationController(IAuthenticationService authService, IJwtTokenService jwtTokenService)
        {
            _authService = authService;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login with email and password to receive JWT token", Tags = new[] { "Authentication" })]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.AuthenticateAsync(request.Email, request.Password);

            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(result.UserId, request.Email, result.UserRole);

            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                userId = result.UserId,
                email = request.Email,
                role = result.UserRole,
                token = token,
                tokenType = "Bearer",
                expiresIn = "3600 seconds",
                instructions = "Use this token in the Authorization header: 'Authorization: Bearer {token}'"
            });
        }

        [HttpPost("validate-token")]
        [SwaggerOperation(Summary = "Validate JWT token", Tags = new[] { "Authentication" })]
        public IActionResult ValidateToken([FromBody] TokenRequest request)
        {
            var principal = _jwtTokenService.ValidateToken(request.Token);

            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid or expired token" });
            }

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var roleClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                isValid = true,
                userId = userIdClaim,
                email = emailClaim,
                role = roleClaim
            });
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}

