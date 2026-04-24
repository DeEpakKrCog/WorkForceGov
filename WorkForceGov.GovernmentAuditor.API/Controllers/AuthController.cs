using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    /// <summary>
    /// STEP 1 — Call this first to get your JWT token.
    /// Then click the Authorize button and enter:  Bearer {token}
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _account;
        private readonly IConfiguration  _config;

        public AuthController(IAccountService account, IConfiguration config)
        {
            _account = account;
            _config  = config;
        }

        /// <summary>
        /// Login with email and password — returns a JWT token valid for 8 hours.
        /// </summary>
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login → get JWT token", Tags = new[] { "Auth" })]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { Message = "Email and Password are required." });

            var (success, message, user) = await _account.LoginAsync(request.Email, request.Password);

            if (!success || user == null)
                return Unauthorized(new { Message = message });

            // Build JWT
            var jwt    = _config.GetSection("Jwt");
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
                new Claim(ClaimTypes.Role,               user.Role),
                new Claim("FullName",                    user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(double.Parse(jwt["ExpiryHours"] ?? "8")),
                signingCredentials: creds
            );

            return Ok(new LoginResponse
            {
                Token     = new JwtSecurityTokenHandler().WriteToken(token),
                UserId    = user.Id,
                FullName  = user.FullName,
                Email     = user.Email,
                Role      = user.Role,
                ExpiresAt = token.ValidTo
            });
        }
    }

    public record LoginRequest(string Email, string Password);
    public record LoginResponse
    {
        public string   Token     { get; init; } = string.Empty;
        public int      UserId    { get; init; }
        public string   FullName  { get; init; } = string.Empty;
        public string   Email     { get; init; } = string.Empty;
        public string   Role      { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}
