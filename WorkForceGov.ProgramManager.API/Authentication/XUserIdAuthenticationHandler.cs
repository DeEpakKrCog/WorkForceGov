using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WorkForceGovProject.Authentication
{
    public class XUserIdAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public XUserIdAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Try to get X-User-Id header
            if (!Request.Headers.TryGetValue("X-User-Id", out var header))
                return Task.FromResult(AuthenticateResult.NoResult());

            var userId = header.ToString();
            if (!int.TryParse(userId, out _))
                return Task.FromResult(AuthenticateResult.Fail("Invalid X-User-Id header."));

            // Create claims and principal
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
