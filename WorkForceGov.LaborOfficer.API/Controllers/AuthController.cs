using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkForceGovProject.Data;
using WorkForceGovProject.Models;

namespace WorkForceGovProject.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;
        public AuthController(ApplicationDbContext db, IConfiguration config, ILogger<AuthController> logger) { _db = db; _config = config; _logger = logger; }

        [HttpPost("token")]
        public IActionResult Token([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
                return BadRequest(new { Message = "Email and Password required" });

            var email = req.Email.Trim();
            var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower() && u.Status == "Active");
            if (user == null)
            {
                _logger.LogInformation("Login failed: no user with email {Email}", email);
                return Unauthorized(new { Message = "Invalid credentials" });
            }
            if (user.Password != req.Password)
            {
                _logger.LogInformation("Login failed: password mismatch for {Email}", email);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var key = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expiresMinutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 60;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                return StatusCode(500, new { Message = "JWT configuration missing on server" });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim("sub", user.Id.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: DateTime.UtcNow.AddMinutes(expiresMinutes), signingCredentials: creds);
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { access_token = tokenStr, token_type = "bearer", expires_in = expiresMinutes * 60 });
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var uidClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub");
            if (uidClaim == null) return Unauthorized(new { Message = "No user claim present" });
            if (!int.TryParse(uidClaim.Value, out var uid)) return Unauthorized(new { Message = "Invalid user id claim" });
            var user = _db.Users.FirstOrDefault(u => u.Id == uid);
            if (user == null) return NotFound(new { Message = "User not found" });
            return Ok(new { user.Id, user.FullName, user.Email, user.Role, user.Status });
        }

        // Developer-only: issue a token for a given email without password (use locally only)
        [HttpGet("devtoken")]
        public IActionResult DevToken([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest(new { Message = "Email required" });
            var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == email.Trim().ToLower() && u.Status == "Active");
            if (user == null) return NotFound(new { Message = "User not found" });

            var key = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expiresMinutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 60;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                return StatusCode(500, new { Message = "JWT configuration missing on server" });

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
                new System.Security.Claims.Claim("role", user.Role),
                new System.Security.Claims.Claim("sub", user.Id.ToString())
            };

            var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: DateTime.UtcNow.AddMinutes(expiresMinutes), signingCredentials: creds);
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new { access_token = tokenStr, token_type = "bearer", expires_in = expiresMinutes * 60 });
        }

        // Debug endpoint: return all claims present in HttpContext.User
        [HttpGet("claims")]
        public IActionResult Claims()
        {
            var claims = User?.Claims.Select(c => new Dictionary<string, string> { { "Type", c.Type }, { "Value", c.Value } }).ToList() ?? new List<Dictionary<string, string>>();
            return Ok(new { Claims = claims });
        }

        // Validate a JWT token and return its claims (accepts token in body or Authorization header)
        [HttpPost("validate")]
        public IActionResult ValidateToken([FromBody] TokenRequest? req)
        {
            string? token = req?.Token;
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }

            if (string.IsNullOrEmpty(token)) return BadRequest(new { Message = "Token not provided" });

            var key = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                return StatusCode(500, new { Message = "JWT configuration is missing on the server" });

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };

                var principal = handler.ValidateToken(token, parameters, out var validatedToken);
                var claims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList();
                return Ok(new { Valid = true, Claims = claims });
            }
            catch (SecurityTokenException ex)
            {
                return Unauthorized(new { Valid = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Valid = false, Message = ex.Message });
            }
        }
    }

    public class LoginRequest { public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }
    public class TokenRequest { public string Token { get; set; } = string.Empty; }
}
