using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace WorkForceGovProject.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;
        private readonly ILogger<TokenValidationMiddleware> _logger;
        public TokenValidationMiddleware(RequestDelegate next, IConfiguration config, ILogger<TokenValidationMiddleware> logger) { _next = next; _config = config; _logger = logger; }
        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            // skip for swagger and auth endpoint
            if (path.StartsWith("/swagger") || path.StartsWith("/api/auth"))
            {
                await _next(context);
                return;
            }

            var auth = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer "))
            {
                var token = auth.Substring("Bearer ".Length).Trim();
                var key = _config["Jwt:Key"];
                var issuer = _config["Jwt:Issuer"];
                var audience = _config["Jwt:Audience"];
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? string.Empty))
                    };
                    var principal = handler.ValidateToken(token, parameters, out var validatedToken);
                    context.User = principal;
                    var idClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
                    _logger.LogInformation("Token validated for user id {UserId}", idClaim);
                }
                catch (SecurityTokenException)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { Message = "Invalid or expired token" });
                    return;
                }
                catch (Exception)
                {
                    // treat unknown errors as unauthorized
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { Message = "Invalid token" });
                    return;
                }
            }

            await _next(context);
        }
    }
}
