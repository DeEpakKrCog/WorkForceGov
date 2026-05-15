using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkForceGovProject.Data; // Assuming this is where ApplicationDbContext lives
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    // Notice: NO [Authorize] here. Everyone must be able to reach the login page!
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        // Inject the database context directly, skipping the service layer
        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        [AllowAnonymous] // Explicitly tells .NET that this endpoint doesn't need a token
        [SwaggerOperation(Summary = "Login → get JWT token", Tags = new[] { "Authentication" })]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { Message = "Email and Password are required." });

            // 1. Direct Database Check (Replaces the AuthenticationService)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Note: Plain string comparison. In production, use BCrypt.Verify()
            if (user == null || user.Password != request.Password)
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            // 2. JWT Configuration Check
            var jwtSecret = _config["Jwt:SecretKey"] ?? _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtSecret))
                return StatusCode(500, new { Message = "JWT configuration is missing in appsettings.json." });

            // 3. Generate Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiryMinutes = double.Parse(_config["Jwt:ExpirationMinutes"] ?? "480"); // 8 Hours

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "WorkForceGov",
                audience: _config["Jwt:Audience"] ?? "WorkForceGovUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            // 4. Return the Token
            return Ok(new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                FullName = user.FullName ?? "",
                ExpiresAt = token.ValidTo
            });
        }
    }

    // Models for Request / Response
    public record LoginRequest(string Email, string Password);

    public record LoginResponse
    {
        public string Token { get; init; } = string.Empty;
        public int UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}