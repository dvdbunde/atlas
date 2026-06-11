using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATLAS.IntegrationTests.Auth;

public class TestAuthHandler : AuthenticationHandler<TestAuthenticationOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<TestAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read identity from X-Test-Identity header sent by TestHttpContextExtensions
        if (Context.Request.Headers.TryGetValue("X-Test-Identity", out var headerValue))
        {
            var value = headerValue.FirstOrDefault();

            // Explicit anonymous signal — return NoResult (no authenticated identity)
            if (value == "ANONYMOUS")
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Base64-encoded identity payload — deserialize and build principal
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var base64 = value;
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                    var dto = JsonSerializer.Deserialize<TestIdentityDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto != null)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, dto.UserId ?? Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.Name, dto.Name ?? "Test User"),
                            new Claim(ClaimTypes.Email, dto.Email ?? "test@atlas.test"),
                        };

                        if (dto.Roles != null)
                        {
                            foreach (var role in dto.Roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }
                        }

                        var identity = new ClaimsIdentity(claims, TestAuthDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(principal, TestAuthDefaults.AuthenticationScheme);
                        return Task.FromResult(AuthenticateResult.Success(ticket));
                    }
                }
                catch
                {
                }
            }
        }

        // Fallback: no test identity header — treat as unauthenticated
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    private class TestIdentityDto
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
    }
}

