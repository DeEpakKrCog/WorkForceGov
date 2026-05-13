using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using WorkForceGovProject.Interfaces.Services;

namespace WorkForceGovProject.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _account;
        private readonly IConfiguration _config;
        private readonly ISystemLogService _logs;

        public AuthController(IAccountService account, IConfiguration config, ISystemLogService logs)
        {
            _account = account;
            _config = config;
            _logs = logs;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login → get JWT token", Tags = new[] { "Auth" })]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { Message = "Email and Password are required." });

            var (success, message, user) = await _account.LoginAsync(request.Email, request.Password);

            if (!success || user == null)
                return Unauthorized(new { Message = message });

            // Log the successful login
            await _logs.LogAsync(user.Id, "UserLogin", $"Role: {user.Role}");

            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(double.Parse(jwt["ExpiryHours"] ?? "8")),
                signingCredentials: creds
            );

            return Ok(new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                ExpiresAt = token.ValidTo
            });
        }

        // ── NEW LOGOUT ENDPOINT ──
        // ── UPDATED LOGOUT ENDPOINT ──
        // ── BULLETPROOF LOGOUT ENDPOINT ──
        [HttpPost("logout")]
        [SwaggerOperation(Summary = "Logout user", Tags = new[] { "Auth" })]
        [ProducesResponseType(typeof(LogoutResponse), 200)]
        public async Task<IActionResult> Logout()
        {
            int userId = 0;

            // 1. Try to get ID from X-User-Id header (Foolproof method)
            if (Request.Headers.TryGetValue("X-User-Id", out var h) && int.TryParse(h, out int p))
            {
                userId = p;
            }
            // 2. Fallback to HttpContext.User
            else if (User.FindFirst(ClaimTypes.NameIdentifier) is Claim c && int.TryParse(c.Value, out int u))
            {
                userId = u;
            }

            // 3. If an ID was found, log it
            if (userId > 0)
            {
                await _logs.LogAsync(userId, "UserLogout", "Session Ended");
            }

            return Ok(new LogoutResponse("Logged out successfully."));
        }
    }

        // --- Records placed inside the namespace but outside the class ---
        public record LoginRequest(string Email, string Password);
    public record LogoutResponse(string Message);

    public record LoginResponse
    {
        public string Token { get; init; } = string.Empty;
        public int UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}