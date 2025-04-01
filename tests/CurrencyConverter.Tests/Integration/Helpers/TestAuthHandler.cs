using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid token"));
        }

        var token = authHeader.Substring("Bearer ".Length).Trim(); // Extract the token

        var claims = new List<Claim>();
        if (token == "mock-token-for-admin")
        {
            claims.Add(new Claim(ClaimTypes.Name, "admin"));
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        else if (token == "mock-token-for-user")
        {
            claims.Add(new Claim(ClaimTypes.Name, "user"));
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token")); // Ensure invalid tokens fail auth
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}